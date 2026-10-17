extends SceneTree

# External QA driver, never included in the public PCK or release assembly.
# Release templates do not support --script. Use the editor host with --main-pack
# and byte-identical release assemblies in an isolated QA workspace; test the native EXE separately.
var session: Node
var game: Node
var failed := false

func _initialize() -> void:
	print("EXPORT_QA_DRIVER_BEGIN")
	call_deferred("run")

func need(ok: bool, detail: String) -> bool:
	if not ok:
		failed = true
		push_error("EXPORT_SMOKE_FAIL " + detail)
		quit(1)
	return ok

func frames(count: int = 3) -> void:
	for _i in range(count):
		await process_frame

func key(code: Key) -> void:
	var down := InputEventKey.new()
	down.keycode = code
	down.physical_keycode = code
	down.pressed = true
	Input.parse_input_event(down)
	var up := InputEventKey.new()
	up.keycode = code
	up.physical_keycode = code
	Input.parse_input_event(up)
	await create_timer(0.19).timeout

func wait_until(predicate: Callable, detail: String) -> bool:
	var started := Time.get_ticks_msec()
	while Time.get_ticks_msec() - started < 10000:
		if predicate.call():
			return true
		await process_frame
	return need(false, "Timed out: " + detail)

func finish_dialogue() -> bool:
	if not await wait_until(func(): return game.get_node("Dialogue").visible, "dialogue begins"):
		return false
	for _i in range(80):
		if not game.get_node("Dialogue").visible:
			return true
		await key(KEY_E)
	return need(false, "Dialogue did not finish with real keyboard events")

func choose(index: int) -> bool:
	if not await wait_until(func(): return game.get_node("Choices").visible, "choice opens"):
		return false
	for _i in range(index):
		await key(KEY_DOWN)
	await key(KEY_ENTER)
	return need(not game.get_node("Choices").visible, "Choice did not close")

func world() -> Node:
	return game.get_node("WorldDisplay/WorldViewport").get_child(0)

func interact(id: String) -> bool:
	var target: Node = world().call("GetTarget", id)
	if not need(target != null, "Missing runtime target " + id):
		return false
	return need(target.call("TryInteract", session), "Rejected runtime target " + id)

func capture(label: String) -> void:
	if DisplayServer.get_name() == "headless":
		return
	await RenderingServer.frame_post_draw
	var directory := ProjectSettings.globalize_path("res://test-output/runtime-captures")
	DirAccess.make_dir_recursive_absolute(directory)
	var result := root.get_texture().get_image().save_png(directory.path_join(label + ".png"))
	need(result == OK, "Unable to capture exported game")

func audit_pack(directory: String = "res://") -> bool:
	for entry in DirAccess.get_files_at(directory):
		var path := directory.path_join(entry)
		for denied in ["res://tests/", "res://scripts/Testing/", "res://references/", "res://tools/", "res://docs/", "res://test-output/", "res://.git/", "res://.superpowers/"]:
			if not need(not path.begins_with(denied), "Private/test resource packed: " + path):
				return false
	for child in DirAccess.get_directories_at(directory):
		if not audit_pack(directory.path_join(child)):
			return false
	return true

func run() -> void:
	session = root.get_node_or_null("GameSession")
	if not need(session != null, "Exported autoload missing"):
		return
	if not need(session.call("get", "ContentError") == "", "Exported content failed to load"):
		return
	if not audit_pack():
		return
	print("EXPORT_PACK_PRIVACY_PASS")
	var boot: Node = load("res://scenes/Boot.tscn").instantiate()
	root.add_child(boot)
	current_scene = boot
	root.grab_focus()
	await frames(5)
	if not need(not boot.get_node("MenuScroll/Menu/StartButton").disabled, "Exported new game disabled"):
		return
	await capture("boot")
	boot.get_node("MenuScroll/Menu/StartButton").pressed.emit()
	await frames(8)
	game = current_scene
	if not need(game != null and game.name == "Main", "Exported new-game scene did not open"):
		return
	# Controlled position/time fixtures exercise the packaged scenes, not human navigation speed.
	session.call("AdvanceClock", 53.0)
	await frames(5)
	if not interact("cannon") or not await finish_dialogue():
		return
	if not interact("street"):
		return
	if not await wait_until(func(): return world().get("SceneId") == "convenience_street" and session.get("Flow") == 0, "street transition"):
		return
	if not interact("hey") or not await finish_dialogue() or not await choose(2) or not await finish_dialogue():
		return
	await capture("street")
	if not interact("soup"):
		return
	if not await wait_until(func(): return world().get("SceneId") == "soup_shop" and session.get("Flow") == 0, "soup transition"):
		return
	world().get_node("DepthLayers/Actors/Player").position = Vector2(400, 430)
	await frames(4)
	if not interact("seat"):
		return
	if not await finish_dialogue() or not await choose(0):
		return
	if not await finish_dialogue() or not await choose(0):
		return
	if not await wait_until(func(): return game.find_child("MemorySoupTable", true, false) != null and session.get("Flow") == 4, "memory transition"):
		return
	var memory := game.find_child("MemorySoupTable", true, false)
	if not need(memory != null, "Packaged memory scene missing"):
		return
	for _i in range(4):
		await key(KEY_E)
	if not await wait_until(func(): return not memory.get_node("Table/Foods/Take").disabled, "coin and bowl movement"):
		return
	if not need(memory.get_node("Status").text.contains("4/4"), "Packaged coin progress incorrect"):
		return
	await capture("memory")
	await key(KEY_E)
	if not await finish_dialogue():
		return
	if not await wait_until(func(): return game.get_node("SliceEnd").visible, "story ending"):
		return
	if not need(game.get_node("SliceEnd/Panel/Scroll/Content/Reflection").text.contains("明天"), "Packaged coda missing"):
		return
	await capture("ending")
	var directory := ProjectSettings.globalize_path("res://test-output")
	DirAccess.make_dir_recursive_absolute(directory)
	var notices := FileAccess.open(directory.path_join("runtime-licenses.json"), FileAccess.WRITE)
	if not need(notices != null, "Unable to record bundled engine notices"):
		return
	notices.store_string(JSON.stringify({"engine_license": Engine.get_license_text(), "third_party_licenses": Engine.get_license_info(), "copyright": Engine.get_copyright_info()}, "\t"))
	notices.close()
	print("EXPORT_SMOKE_PASS Boot NewGame Invitation Street Candy Soup Memory Keyboard Ending")
	quit(0)
