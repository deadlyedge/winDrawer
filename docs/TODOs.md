# todos

- 是否需要添加一个公共文字处理方法或类
  - 判断背景浅色用深色字体，背景深色用浅色字体。
  - 深浅色应该有一个阈值，可通过调整阈值来调试程序显示效果
  - 主窗口logo也应该可以切换深浅色
- 初始化时（没找到drawer-settings.toml）应
  - 创建一个默认抽屉，指向user/.config/winDrawer
  - 创建另一个默认抽屉，指向user/.config/winDrawer/demoDrawer，并放入几个文件‘可将文件拖入抽屉.txt’，‘可预览图片（程序logo）’，‘双击打开文件夹’
- 在settings窗口添加一个指向github的链接
- 设置中的lock position应改为lock window，lock时应隐藏主窗口的添加和设置按钮，以最大程度缩小可点击区域。设置可通过trayiconmenu进入


# thoughts (not sure)

- drawerSetting是否可以定义defaultOpenWith？用来定义默认双击打开行为。
- 主窗口的设置图标按钮应为switch，再次点击可以关闭设置窗口
- 主窗口应该可以支持拖放，拖入文件夹时添加抽屉，拖入文件时添加上层文件夹为抽屉
