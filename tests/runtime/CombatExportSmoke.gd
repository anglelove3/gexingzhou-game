extends SceneTree

# Read-only combat telemetry drives normal keyboard input in the release PCK.
# This driver stays outside the public PCK and release assembly.
var game: Node
var pressed: Dictionary = {}

func _initialize() -> void:
	call_deferred("run")

func need(ok: bool, message: String) -> bool:
	if not ok:
		push_error("EXPORT_COMBAT_FAIL " + message)
		quit(1)
	return ok

func frames(count := 1) -> void:
	for _i in range(count):
		await process_frame

func set_key(code: Key, down: bool, pulse := false) -> void:
	if pressed.has(code) and (not down or pulse):
		var up := InputEventKey.new()
		up.keycode = code
		up.physical_keycode = code
		Input.parse_input_event(up)
		pressed.erase(code)
	if down and not pressed.has(code):
		var event := InputEventKey.new()
		event.keycode = code
		event.physical_keycode = code
		event.pressed = true
		Input.parse_input_event(event)
		pressed[code] = true

func key(code: Key) -> void:
	set_key(code, true, true)
	set_key(code, false)
	await frames(3)

func release_keys() -> void:
	for code: int in pressed.keys():
		set_key(code as Key, false)

func audit_pack(directory := "res://") -> bool:
	for entry in DirAccess.get_files_at(directory):
		var path := directory.path_join(entry)
		for denied in ["res://tests/", "res://scripts/Testing/", "res://references/", "res://tools/", "res://docs/", "res://test-output/", "res://.git/"]:
			if not need(not path.begins_with(denied), "Private/test resource packed " + path):
				return false
	for child in DirAccess.get_directories_at(directory):
		if not audit_pack(directory.path_join(child)):
			return false
	return true

func capture(label: String) -> void:
	await frames(3)
	# An occluded desktop window can suspend frame_post_draw indefinitely.
	RenderingServer.force_draw()
	var directory := ProjectSettings.globalize_path("res://test-output/combat-packed-captures")
	DirAccess.make_dir_recursive_absolute(directory)
	var pixels := root.get_texture().get_image()
	if need(not pixels.is_empty(), "Empty rendered frame"):
		need(pixels.save_png(directory.path_join(label + ".png")) == OK, "Could not save render")

func state() -> Dictionary:
	return game.call("ReadCombatState")

func decide(info: Dictionary) -> Dictionary:
	var target: Dictionary = {}
	var distance := INF
	for enemy: Dictionary in info.enemies:
		if int(enemy.health) <= 0:
			continue
		var gap: float = absf(float(enemy.x) - float(info.x)) + absf(float(enemy.y) - float(info.y)) * 2.0
		if gap < distance:
			distance = gap
			target = enemy
	if target.is_empty():
		return {}
	var dx: float = float(target.x) - float(info.x)
	var dy: float = float(target.y) - float(info.y)
	var move_x: float = signf(dx) if absf(dx) > 42.0 else signf(dx) * 0.05 if int(info.facing) != int(signf(dx)) else 0.0
	var move_y: float = clampf(dy / 18.0, -1.0, 1.0) if absf(dy) > 7.0 else 0.0
	var input := {"x": move_x, "y": move_y, "light": false, "heavy_press": false, "heavy_hold": false, "guard": false}
	var can_act: bool = info.state in ["Idle", "Guard"]
	if can_act and float(info.stamina) < 42.0:
		input.x = 1.0 if float(info.x) < 140.0 else -1.0 if float(info.x) > 1140.0 else -signf(dx)
		input.y = 1.0 if int(info.tick) % 300 < 150 else -1.0
		return input
	if info.state == "Charging":
		input.heavy_hold = int(info.frame) < 28
		return input
	if info.state == "Attack":
		input.light = str(info.attack).begins_with("light") and int(info.total) - int(info.frame) <= 7 and absf(dx) < 75.0
		return input
	if not can_act:
		return {}
	for enemy: Dictionary in info.enemies:
		if enemy.id == "leader" and enemy.state == "Attack" and int(enemy.frame) < int(enemy.active_end) and absf(float(enemy.x) - float(info.x)) <= 75.0 and absf(float(enemy.y) - float(info.y)) <= 26.0:
			input.x = signf(float(enemy.x) - float(info.x)) * 0.05 if int(info.facing) != int(signf(float(enemy.x) - float(info.x))) else 0.0
			input.y = 0.0
			input.guard = true
			return input
	var close: bool = absf(dx) <= 64.0 and absf(dy) <= 18.0
	var heavy: bool = target.state in ["Guard", "Down", "Rising"] or target.id == "leader" and (target.state == "Recovery" or target.state == "Attack" and int(target.frame) >= int(target.active_end))
	input.light = close and not heavy
	input.heavy_press = close and heavy
	input.heavy_hold = close and heavy
	return input

func apply_input(input: Dictionary) -> void:
	set_key(KEY_A, float(input.get("x", 0.0)) < 0.0)
	set_key(KEY_D, float(input.get("x", 0.0)) > 0.0)
	set_key(KEY_W, float(input.get("y", 0.0)) < 0.0)
	set_key(KEY_S, float(input.get("y", 0.0)) > 0.0)
	set_key(KEY_J, input.get("light", false), input.get("light", false))
	set_key(KEY_K, input.get("heavy_press", false) or input.get("heavy_hold", false), input.get("heavy_press", false))
	set_key(KEY_L, input.get("guard", false))

func enter_combat(boot: Node) -> void:
	boot.get_node("MenuScroll/Menu/CombatButton").grab_focus()
	await key(KEY_ENTER)
	await frames(5)
	game = current_scene

func run() -> void:
	if not audit_pack():
		return
	print("EXPORT_COMBAT_PRIVACY_PASS")
	var boot: Node = load("res://scenes/Boot.tscn").instantiate()
	root.add_child(boot)
	current_scene = boot
	root.grab_focus()
	await frames(5)
	await enter_combat(boot)
	if not need(game.has_method("ReadCombatState") and state().screen == "prepare", "Menu keyboard combat entry failed"):
		return
	if not need(int(state().experience) == 0, "Expected isolated fresh combat progress"):
		return
	await capture("combat-prepare")
	var expected_xp := [30, 60, 110, 160, 300, 480]
	for index in range(6):
		for id in ["stick", "padded", "steady"]:
			var equipment: Button = game.get_node("Prepare/Panel/Scroll/Content/Equipment/" + id)
			if not equipment.disabled:
				equipment.grab_focus()
				await key(KEY_ENTER)
		game.get_node("Prepare/Footer/Content/Actions/Start").grab_focus()
		await key(KEY_ENTER)
		var info := state()
		if not need(info.screen == "fight", "Starting wave failed " + str(index)):
			return
		for tick in range(12000):
			info = state()
			if info.screen == "result":
				break
			if info.screen == "pause":
				release_keys()
				root.grab_focus()
				await frames(3)
				game.get_node("Pause/Panel/Scroll/Content/Resume").grab_focus()
				await key(KEY_ENTER)
				continue
			apply_input(decide(info))
			await frames()
			if tick == 100:
				await capture("combat-wave-" + str(index + 1))
		release_keys()
		if not need(state().screen == "result" and int(state().experience) == expected_xp[index], "Wave failed or growth missing " + str(index)):
			return
		print("EXPORT_COMBAT_WAVE_PASS " + str(index + 1) + " xp=" + str(state().experience))
		await capture("combat-result-" + str(index + 1))
		game.get_node("Result/Panel/Scroll/Content/Continue").grab_focus()
		await key(KEY_ENTER)
		if not need(state().screen == "prepare", "Continue result failed"):
			return
	for id in ["wraps", "gloves", "stick", "jacket", "canvas", "padded", "trainers", "light", "steady"]:
		if not need(not game.get_node("Prepare/Panel/Scroll/Content/Equipment/" + id).disabled, "Missing equipment " + id):
			return
	await capture("combat-level5-equipment")
	game.get_node("Prepare/Footer/Content/Actions/Menu").grab_focus()
	await key(KEY_ENTER)
	await frames(5)
	await enter_combat(current_scene)
	if not need(int(state().experience) == 480, "Saved combat progress did not resume after menu"):
		return
	print("EXPORT_COMBAT_PASS MenuKeyboard SixWaves Level5 NineItems Equipment SavedResume")
	var session := root.get_node("GameSession")
	session.call("RequestQuit")
