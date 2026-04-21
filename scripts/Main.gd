extends Control

const TASK_NAME := "SCUNET Auto Login"
const DEFAULT_DIR := "D:/scu/release_package"
const LOGIN_EXE := "scu_net_auto_login.exe"
const CONFIG_FILE := "Config/setting.json"
const TOOL_CONFIG_FILE := "Config/scunet_config_tool.json"
const DEFAULT_STARTUP_DELAY := 10
const DEFAULT_INITIAL_DELAY := 5
const DEFAULT_RETRY_COUNT := 1
const DEFAULT_RETRY_DELAY := 5
const TEXT_COLOR := Color(0.05, 0.07, 0.09)
const HINT_COLOR := Color(0.12, 0.16, 0.20)
const OK_COLOR := Color(0.00, 0.30, 0.16)
const WARN_COLOR := Color(0.55, 0.09, 0.07)

var release_dir := ""
var config := {}

var dir_edit: LineEdit
var username_edit: LineEdit
var password_edit: LineEdit
var service_box: OptionButton
var startup_delay_spin: SpinBox
var initial_delay_spin: SpinBox
var retry_count_spin: SpinBox
var retry_delay_spin: SpinBox
var status_label: Label
var task_label: Label


func _ready() -> void:
	release_dir = _find_release_dir()
	_build_ui()
	_load_config()
	_refresh_task()


func _build_ui() -> void:
	var background := ColorRect.new()
	background.color = Color(0.98, 0.98, 0.97)
	background.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(background)

	var margin := MarginContainer.new()
	margin.set_anchors_preset(Control.PRESET_FULL_RECT)
	margin.add_theme_constant_override("margin_left", 28)
	margin.add_theme_constant_override("margin_top", 24)
	margin.add_theme_constant_override("margin_right", 28)
	margin.add_theme_constant_override("margin_bottom", 24)
	add_child(margin)

	var box := VBoxContainer.new()
	box.add_theme_constant_override("separation", 14)
	margin.add_child(box)

	box.add_child(_label("SCUNET 配置器", 28))
	box.add_child(_hint("开源项目取自https://github.com/LFWQSP2641/scu_net_auto_login，本程序为简便项目使用的配置器"))

	var grid := GridContainer.new()
	grid.columns = 2
	grid.add_theme_constant_override("h_separation", 12)
	grid.add_theme_constant_override("v_separation", 12)
	box.add_child(grid)

	grid.add_child(_label("登录器目录", 16))
	var dir_row := HBoxContainer.new()
	dir_row.add_theme_constant_override("separation", 8)
	dir_edit = LineEdit.new()
	dir_edit.text = release_dir.replace("/", "\\")
	dir_edit.placeholder_text = "例如 D:\\scu\\release_package"
	dir_edit.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	dir_row.add_child(dir_edit)
	_add_button(dir_row, "读取目录", _apply_directory)
	grid.add_child(dir_row)

	grid.add_child(_label("账号", 16))
	username_edit = LineEdit.new()
	username_edit.placeholder_text = "校园网账号"
	grid.add_child(username_edit)

	grid.add_child(_label("密码", 16))
	password_edit = LineEdit.new()
	password_edit.placeholder_text = "密码"
	password_edit.secret = true
	grid.add_child(password_edit)

	grid.add_child(_label("运营商", 16))
	service_box = OptionButton.new()
	_add_service("校园网", "EDUNET")
	_add_service("中国电信", "CHINATELECOM")
	_add_service("中国移动", "CHINAMOBILE")
	_add_service("中国联通", "CHINAUNICOM")
	grid.add_child(service_box)

	grid.add_child(_label("开机后延迟", 16))
	startup_delay_spin = _spin(DEFAULT_STARTUP_DELAY, 0, 600, " 秒")
	grid.add_child(startup_delay_spin)

	grid.add_child(_label("登录前等待", 16))
	initial_delay_spin = _spin(DEFAULT_INITIAL_DELAY, 0, 300, " 秒")
	grid.add_child(initial_delay_spin)

	grid.add_child(_label("重试次数", 16))
	retry_count_spin = _spin(DEFAULT_RETRY_COUNT, 1, 10, " 次")
	grid.add_child(retry_count_spin)

	grid.add_child(_label("重试间隔", 16))
	retry_delay_spin = _spin(DEFAULT_RETRY_DELAY, 1, 120, " 秒")
	grid.add_child(retry_delay_spin)

	var buttons := HBoxContainer.new()
	buttons.add_theme_constant_override("separation", 8)
	box.add_child(buttons)
	_add_button(buttons, "保存配置", _save_config)
	_add_button(buttons, "测试登录", _test_login)
	_add_button(buttons, "启用自启", _enable_startup)
	_add_button(buttons, "取消自启", _disable_startup)

	task_label = _hint("自启状态：检查中")
	box.add_child(task_label)

	status_label = _hint("")
	status_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	box.add_child(status_label)


func _label(text: String, size: int) -> Label:
	var label := Label.new()
	label.text = text
	label.add_theme_font_size_override("font_size", size)
	label.add_theme_color_override("font_color", TEXT_COLOR)
	label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	return label


func _hint(text: String) -> Label:
	var label := _label(text, 14)
	label.add_theme_color_override("font_color", HINT_COLOR)
	return label


func _add_button(parent: Control, text: String, callback: Callable) -> void:
	var button := Button.new()
	button.text = text
	button.add_theme_color_override("font_color", TEXT_COLOR)
	button.add_theme_color_override("font_hover_color", TEXT_COLOR)
	button.add_theme_color_override("font_pressed_color", TEXT_COLOR)
	button.add_theme_color_override("font_focus_color", TEXT_COLOR)
	button.custom_minimum_size = Vector2(120, 36)
	button.pressed.connect(callback)
	parent.add_child(button)


func _spin(value: int, min_value: int, max_value: int, suffix: String) -> SpinBox:
	var spin := SpinBox.new()
	spin.min_value = min_value
	spin.max_value = max_value
	spin.step = 1
	spin.value = value
	spin.suffix = suffix
	spin.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	return spin


func _add_service(label: String, value: String) -> void:
	var index := service_box.item_count
	service_box.add_item(label)
	service_box.set_item_metadata(index, value)


func _find_release_dir() -> String:
	var exe_dir := OS.get_executable_path().get_base_dir()
	if FileAccess.file_exists(_join(exe_dir, LOGIN_EXE)):
		return exe_dir
	return DEFAULT_DIR


func _load_config() -> void:
	_sync_release_dir()
	var path := _join(release_dir, CONFIG_FILE)
	config = _read_json(path)
	if config.is_empty():
		config = _default_config()

	var user := _first_user()
	username_edit.text = str(user.get("username", ""))
	password_edit.text = str(user.get("password", ""))
	_select_service(str(user.get("service", "EDUNET")))
	startup_delay_spin.value = _load_startup_delay()
	initial_delay_spin.value = int(config.get("initialDelay", DEFAULT_INITIAL_DELAY))
	retry_count_spin.value = int(config.get("retryCount", DEFAULT_RETRY_COUNT))
	retry_delay_spin.value = int(config.get("retryDelay", DEFAULT_RETRY_DELAY))
	if FileAccess.file_exists(_join(release_dir, LOGIN_EXE)):
		_set_status("当前目录：" + release_dir.replace("/", "\\"))
	else:
		_set_status("找不到登录器，请手动输入目录", true)


func _apply_directory() -> void:
	_sync_release_dir()
	_load_config()
	_refresh_task()


func _read_json(path: String) -> Dictionary:
	if not FileAccess.file_exists(path):
		return {}

	var file := FileAccess.open(path, FileAccess.READ)
	if file == null:
		return {}

	var json := JSON.new()
	if json.parse(file.get_as_text()) != OK:
		return {}

	return json.data if typeof(json.data) == TYPE_DICTIONARY else {}


func _write_json(path: String, data: Dictionary) -> bool:
	var dir := path.get_base_dir()
	var error := DirAccess.make_dir_recursive_absolute(dir)
	if error != OK:
		return false

	var file := FileAccess.open(path, FileAccess.WRITE)
	if file == null:
		return false

	file.store_string(JSON.stringify(data, "    ", false))
	return true


func _default_config() -> Dictionary:
	return {
		"enableConnectSCUNETWifi": false,
		"enableHotspot": false,
		"initialDelay": DEFAULT_INITIAL_DELAY,
		"retryCount": DEFAULT_RETRY_COUNT,
		"retryDelay": DEFAULT_RETRY_DELAY,
		"userList": [{"username": "", "password": "", "service": "EDUNET"}]
	}


func _first_user() -> Dictionary:
	var users = config.get("userList", [])
	if typeof(users) != TYPE_ARRAY or users.is_empty() or typeof(users[0]) != TYPE_DICTIONARY:
		config["userList"] = [{"username": "", "password": "", "service": "EDUNET"}]
	return config["userList"][0]


func _save_config() -> bool:
	_sync_release_dir()
	if not FileAccess.file_exists(_join(release_dir, LOGIN_EXE)):
		_set_status("找不到 " + LOGIN_EXE, true)
		return false

	var user := _first_user()
	user["username"] = username_edit.text.strip_edges()
	user["password"] = password_edit.text
	user["service"] = _selected_service()
	config["userList"][0] = user
	config["initialDelay"] = int(initial_delay_spin.value)
	config["retryCount"] = int(retry_count_spin.value)
	config["retryDelay"] = int(retry_delay_spin.value)

	DirAccess.make_dir_recursive_absolute(_join(release_dir, "Config"))
	var file := FileAccess.open(_join(release_dir, CONFIG_FILE), FileAccess.WRITE)
	if file == null:
		_set_status("保存失败：" + error_string(FileAccess.get_open_error()), true)
		return false

	file.store_string(JSON.stringify(config, "    ", false))
	_save_tool_config()
	_set_status("配置已保存")
	return true


func _test_login() -> void:
	if not _save_config():
		return
	var exe := _join(release_dir, LOGIN_EXE).replace("/", "\\")
	var dir := release_dir.replace("/", "\\")
	var command := "Start-Process -FilePath '%s' -WorkingDirectory '%s'" % [_ps(exe), _ps(dir)]
	OS.create_process("powershell.exe", ["-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", command], false)
	_set_status("已启动登录器")


func _enable_startup() -> void:
	_write_task_script(true)


func _disable_startup() -> void:
	_write_task_script(false)


func _write_task_script(create_task: bool) -> void:
	_sync_release_dir()
	if create_task and not FileAccess.file_exists(_join(release_dir, LOGIN_EXE)):
		_set_status("找不到 " + LOGIN_EXE, true)
		return

	_save_tool_config()

	var temp_dir := _join(release_dir, "Temp")
	DirAccess.make_dir_recursive_absolute(temp_dir)
	var script_path := _join(temp_dir, "scunet_startup_task.ps1")
	var file := FileAccess.open(script_path, FileAccess.WRITE)
	if file == null:
		_set_status("无法创建临时脚本", true)
		return

	file.store_string(_task_create_script() if create_task else _task_delete_script())
	var command := "Start-Process powershell.exe -Verb RunAs -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-File','%s')" % _ps(script_path)
	OS.create_process("powershell.exe", ["-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", command], false)
	_set_status("请在管理员确认后刷新或重新打开配置器")


func _task_create_script() -> String:
	return """$ErrorActionPreference = 'Stop'
$taskName = '%s'
$exePath = '%s'
$workDir = '%s'
$userSid = [System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value
$delaySeconds = %s
$delayNode = if ($delaySeconds -gt 0) { "<Delay>PT${delaySeconds}S</Delay>" } else { "" }
$xml = @"
<?xml version="1.0" encoding="UTF-16"?>
<Task version="1.3" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
  <RegistrationInfo>
    <URI>\\$taskName</URI>
  </RegistrationInfo>
  <Principals>
    <Principal id="Author">
      <UserId>$userSid</UserId>
      <LogonType>InteractiveToken</LogonType>
    </Principal>
  </Principals>
  <Settings>
    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
    <StartWhenAvailable>true</StartWhenAvailable>
  </Settings>
  <Triggers>
    <LogonTrigger>
      $delayNode
    </LogonTrigger>
  </Triggers>
  <Actions Context="Author">
    <Exec>
      <Command>$exePath</Command>
      <WorkingDirectory>$workDir</WorkingDirectory>
    </Exec>
  </Actions>
</Task>
"@
Register-ScheduledTask -TaskName $taskName -Xml $xml -Force | Out-Null
""" % [_ps(TASK_NAME), _ps(_join(release_dir, LOGIN_EXE).replace("/", "\\")), _ps(release_dir.replace("/", "\\")), str(int(startup_delay_spin.value))]


func _task_delete_script() -> String:
	return "Unregister-ScheduledTask -TaskName '%s' -Confirm:$false\n" % _ps(TASK_NAME)


func _refresh_task() -> void:
	var output: Array = []
	var code := OS.execute("schtasks.exe", ["/Query", "/TN", TASK_NAME], output, true, false)
	task_label.text = "自启状态：已启用" if code == 0 else "自启状态：未启用"
	task_label.add_theme_color_override("font_color", OK_COLOR if code == 0 else WARN_COLOR)


func _read_task_delay_seconds() -> int:
	var output: Array = []
	var command := "$xml = Export-ScheduledTask -TaskName '%s' -ErrorAction SilentlyContinue; if($xml){$doc=[xml]$xml; $ns=New-Object System.Xml.XmlNamespaceManager($doc.NameTable); $ns.AddNamespace('t','http://schemas.microsoft.com/windows/2004/02/mit/task'); $delayNode=$doc.SelectSingleNode('//t:LogonTrigger/t:Delay',$ns); if($delayNode){$delayNode.InnerText}else{'PT0S'}} else {'__NO_TASK__'}" % _ps(TASK_NAME)
	var code := OS.execute("powershell.exe", ["-NoProfile", "-Command", command], output, true, false)
	if code != 0 or output.is_empty():
		return DEFAULT_STARTUP_DELAY

	var value := str(output[0]).strip_edges()
	if value == "__NO_TASK__":
		return DEFAULT_STARTUP_DELAY

	return _parse_iso_seconds(value, DEFAULT_STARTUP_DELAY)


func _parse_iso_seconds(value: String, fallback: int) -> int:
	if value == "":
		return fallback

	var regex := RegEx.new()
	if regex.compile("^PT(?:(\\d+)H)?(?:(\\d+)M)?(?:(\\d+)S)?$") != OK:
		return fallback

	var match := regex.search(value)
	if match == null:
		return fallback

	var hours := int(match.get_string(1)) if match.get_string(1) != "" else 0
	var minutes := int(match.get_string(2)) if match.get_string(2) != "" else 0
	var seconds := int(match.get_string(3)) if match.get_string(3) != "" else 0
	return hours * 3600 + minutes * 60 + seconds


func _load_tool_config() -> Dictionary:
	return _read_json(_join(release_dir, TOOL_CONFIG_FILE))


func _save_tool_config() -> void:
	_write_json(_join(release_dir, TOOL_CONFIG_FILE), {
		"startupDelaySeconds": int(startup_delay_spin.value)
	})


func _load_startup_delay() -> int:
	var tool_config := _load_tool_config()
	if tool_config.has("startupDelaySeconds"):
		return int(tool_config.get("startupDelaySeconds", DEFAULT_STARTUP_DELAY))

	var task_delay := _read_task_delay_seconds()
	if task_delay != DEFAULT_STARTUP_DELAY:
		return task_delay

	return DEFAULT_STARTUP_DELAY


func _selected_service() -> String:
	var index := service_box.selected
	return str(service_box.get_item_metadata(index)) if index >= 0 else "EDUNET"


func _select_service(value: String) -> void:
	for index in service_box.item_count:
		if str(service_box.get_item_metadata(index)) == value:
			service_box.select(index)
			return
	service_box.select(0)


func _sync_release_dir() -> void:
	if dir_edit == null:
		return

	release_dir = dir_edit.text.strip_edges().replace("\\", "/").rstrip("/\\")
	dir_edit.text = release_dir.replace("/", "\\")


func _set_status(text: String, is_error := false) -> void:
	status_label.text = text
	status_label.add_theme_color_override("font_color", WARN_COLOR if is_error else OK_COLOR)


func _join(base: String, child: String) -> String:
	return base.rstrip("/\\") + "/" + child


func _ps(value: String) -> String:
	return value.replace("'", "''")
