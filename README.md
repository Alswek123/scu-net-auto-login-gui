# SCUNET Config Tool

一个给 Windows 用户使用的图形化配置器，用来为
[`LFWQSP2641/scu_net_auto_login`](https://github.com/LFWQSP2641/scu_net_auto_login)
生成和修改配置，并管理开机自启。

这个项目 **不重写登录逻辑**。真正执行校园网登录的仍然是上游项目里的：

```text
scu_net_auto_login.exe
```

本项目负责的内容：

- 填写账号、密码、运营商
- 读取和写入 `Config/setting.json`
- 调整开机后延迟、登录前等待、重试次数、重试间隔
- 启用或取消 Windows 开机自启
- 测试运行 `scu_net_auto_login.exe`
- 在登录器目录无法自动识别时，允许手动输入目录

## 项目定位

适合不想手改 JSON、也不想记命令行参数的普通用户。

推荐与上游 Windows 发布包放在同一目录：

```text
release_package/
├─ SCUNET_Config_Tool.exe
├─ scu_net_auto_login.exe
├─ Config/
│  ├─ setting.json
│  └─ scunet_config_tool.json
```

## 与上游项目的关系

- 上游项目：四川大学校园网自动登录核心逻辑
- 本项目：GUI 配置器
- 上游仓库地址：
  [LFWQSP2641/scu_net_auto_login](https://github.com/LFWQSP2641/scu_net_auto_login)

如果你只需要命令行或手改配置文件，请直接使用上游项目。

## 当前功能

- 手动输入登录器目录
- 填写账号和密码
- 选择运营商
- 调整开机后延迟
- 调整登录前等待
- 调整重试次数
- 调整重试间隔
- 保存配置
- 测试登录
- 启用或取消开机自启

## 开发环境

- Godot 4.6
- Windows

## 导出

本项目导出的是配置器本体，不包含上游登录源码。

当前常用导出命令示例：

```powershell
& 'C:\Program Files (x86)\Steam\steamapps\common\Godot Engine\godot.windows.opt.tools.64.exe' `
  --headless `
  --path 'C:\Users\13892\Documents\启动器' `
  --export-release 'Windows Desktop' `
  'D:\scu\release_package\SCUNET_Config_Tool.exe'
```

## 发布建议

建议将仓库源码与打包文件分开：

- GitHub 仓库：放 Godot 工程源码
- GitHub Releases：放预编译好的 `release_package.zip / .rar / .7z`

## License

本项目源码采用 MIT License。

上游项目和第三方依赖的许可证请分别以它们自己的说明为准。
