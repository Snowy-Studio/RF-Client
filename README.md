# 红色警戒2 重聚未来 1.5 开发者文档
> [!CAUTION]
> ## 重聚未来制作组不允许任何基于重聚且名字和内容高度相似的三创衍生作品公开发布，这里公开点名某个叫知寻2027的，可以自用、测试或借鉴开发其他红警类型的二创作品
> ### 对此评价：改的一坨，就单说任务重复文件一堆，初始分发文件不纯净等问题，何尝不是红警版的"小而美"？
> ### 同时存在不遵守OSI开源协议(虽然是君子协议，但这里还是要说：GPL协议要求基于GPL协议项目再次开发的项目必须以同样的协议开源)，不尊重作者版权等一系列问题，RA2圈内不欢迎先上线后授权的模式
> ### 举个例子: 开发者A把一个资源授权给了开发者B，则只允许在开发者B的项目内使用，如果开发者C基于开发者B的项目再次使用开发者A授权的内容，则≠开发者A把资源同时授权给了开发者C，此时开发者C需要重新向开发者A索要资源使用授权，如果开发者C未向开发者A索要授权则构成侵权

<div align="center">

![image](https://socialify.git.ci/Snowy-Studio/RF-Client/image?description=1&font=Inter&forks=1&issues=1&language=1&name=1&owner=1&pulls=1&stargazers=1&theme=Auto)

**本项目基于: [xna-cncnet-client](https://github.com/CnCNet/xna-cncnet-client)**

</div>

## 项目结构

### ClientCore

这里放的是一些底层的数据结构和方法.

### ClientGUI

这里放的是三次开发的UI控件.

### ClientUpdater

这里是更新程序，独立于客户端的.

### DTAConfig

这里放的主要是设置界面的配置窗口.

### Localization

这里实现了多语言功能和一些扩展方法.

### RA2Client

客户端主要项目，大部分界面的实现和业务逻辑位于此.

### Rampastring.Tools-2.0.6

读写INI和一些封装的方法实现.

### Rampastring.XNAUI

这里放的是二次开发的UI控件.

### RandomMapGenerator

这是用来生成随机地图的项目.

### Reunion

这是用于生成启动游戏的exe.

### CnCNet License

CnCNet Client
Copyright (C) 2022-2024 CnCNet, Rampastring

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
