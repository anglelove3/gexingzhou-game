extends SceneTree

# Run each case in a separate engine process with --suite=Shutdown to isolate saves.
# A successful quit must finish itself; the launcher enforces a process timeout.
var mode := "menu"
var audio: Node
var request_started := 0

func need(condition: bool, detail: String) -> bool:
	if not condition:
		push_error("SHUTDOWN_CHECK_FAIL " + detail)
		quit(1)
	return condition

func _initialize() -> void:
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--shutdown-mode="):
			mode = arg.trim_prefix("--shutdown-mode=")
	call_deferred("run")

func run() -> void:
	var session = root.get_node("GameSession")
	var scene = load("res://scenes/Boot.tscn" if mode in ["menu", "muted", "window-menu"] else "res://scenes/Main.tscn").instantiate()
	root.add_child(scene)
	for i in range(5):
		await process_frame
	audio = scene.get_node("Audio")
	if not need(not scene.has_meta("binding_error"), "scene bindings"):
		return
	if mode == "muted":
		scene.get_node("Settings").call("Open")
		scene.get_node("Settings/Panel/Scroll/Content/MasterVolume").value = 0
		for i in range(3):
			await process_frame
		if not need(audio.get_node("Music").stream != null and audio.get_node("Music").stream_paused and audio.get_node("Ambience").stream_paused, "real volume-zero setting pauses active streams"):
			return
		scene.get_node("Settings").call("Close")
	if mode == "effect":
		if not need(audio.call("PlayCue", 2), "phone ring playing before exit"):
			return
	if mode in ["pause", "save-failure"]:
		if not need(scene.get_node("Pause").call("Open"), "pause opens"):
			return
	if mode == "save-failure":
		var save_path = session.get("SaveDirectory").path_join("save.json")
		if not need(save_path.begins_with(ProjectSettings.globalize_path("res://test-output/")), "isolated save failure fixture"):
			return
		DirAccess.make_dir_recursive_absolute(save_path)
		if not need(not scene.get_node("Pause").call("TryExit"), "save failure blocks exit"):
			return
		if not need(not paused and audio.get_node("Music").playing, "save failure keeps current scene and audio alive"):
			return
		if not need(scene.get_node("Pause/Panel/Scroll/Content/Message").visible, "save failure remains visible"):
			return
		DirAccess.remove_absolute(save_path)
	request_started = Time.get_ticks_msec()
	if mode in ["menu", "muted"]:
		scene.get_node("MenuScroll/Menu/QuitButton").emit_signal("pressed")
	elif mode in ["pause", "save-failure"]:
		if not need(scene.get_node("Pause").call("TryExit"), "save and exit accepted"):
			return
		if not need(FileAccess.file_exists(session.get("SaveDirectory").path_join("save.json")), "checkpoint saved before exit"):
			return
	else:
		if not need(not auto_accept_quit, "window close uses graceful shutdown"):
			return
		root.emit_signal("close_requested")
	if not need(paused, "gameplay frozen during shutdown"):
		return
	if not need(not audio.get_node("Music").playing and not audio.get_node("Ambience").playing, "audio stopped before engine teardown"):
		return
	if not need(not audio.call("PlayCue", 0), "late audio cue rejected"):
		return
	# Duplicate requests must preserve the same shutdown and save outcome.
	session.call("RequestQuit")
	print("SHUTDOWN_CHECK_PASS ", mode)
	while Time.get_ticks_msec() - request_started < 2500:
		await process_frame
	need(false, "shutdown failed to finish within 2.5 seconds")
