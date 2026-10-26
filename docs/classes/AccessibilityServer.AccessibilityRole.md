# AccessibilityServer.AccessibilityRole

Last updated: 2026-10-26

- Declaration: `public enum AccessibilityServer.AccessibilityRole`
- Source: [AccessibilityServer.Enums.cs](../../src/Servers/Accessibility/AccessibilityServer.Enums.cs)
- Owner: [AccessibilityServer](AccessibilityServer.md)

## Description

Semantic role hints project to native role identities. Aliases between different toolkit vocabularies are intentional; assigning a role does not create a text/value/table implementation.

## Values

| Value | Number | Meaning |
| --- | ---: | --- |
| <a id="unknown"></a>`Unknown` | 0 | Identifies the unknown role. |
| <a id="defaultbutton"></a>`DefaultButton` | 1 | Identifies the default button role. |
| <a id="audio"></a>`Audio` | 2 | Identifies the audio role. |
| <a id="video"></a>`Video` | 3 | Identifies the video role. |
| <a id="statictext"></a>`StaticText` | 4 | Identifies the static text role. |
| <a id="container"></a>`Container` | 5 | Identifies the container role. |
| <a id="panel"></a>`Panel` | 6 | Identifies the panel role. |
| <a id="button"></a>`Button` | 7 | Identifies the button role. |
| <a id="link"></a>`Link` | 8 | Identifies the link role. |
| <a id="checkbox"></a>`CheckBox` | 9 | Identifies the check box role. |
| <a id="radiobutton"></a>`RadioButton` | 10 | Identifies the radio button role. |
| <a id="checkbutton"></a>`CheckButton` | 11 | Identifies the check button role. |
| <a id="scrollbar"></a>`ScrollBar` | 12 | Identifies the scroll bar role. |
| <a id="scrollview"></a>`ScrollView` | 13 | Identifies the scroll view role. |
| <a id="splitter"></a>`Splitter` | 14 | Identifies the splitter role. |
| <a id="slider"></a>`Slider` | 15 | Identifies the slider role. |
| <a id="spinbutton"></a>`SpinButton` | 16 | Identifies the spin button role. |
| <a id="progressindicator"></a>`ProgressIndicator` | 17 | Identifies the progress indicator role. |
| <a id="textfield"></a>`TextField` | 18 | Identifies the text field role. |
| <a id="multilinetextfield"></a>`MultilineTextField` | 19 | Identifies the multiline text field role. |
| <a id="colorpicker"></a>`ColorPicker` | 20 | Identifies the color picker role. |
| <a id="table"></a>`Table` | 21 | Identifies the table role. |
| <a id="cell"></a>`Cell` | 22 | Identifies the cell role. |
| <a id="row"></a>`Row` | 23 | Identifies the row role. |
| <a id="rowgroup"></a>`RowGroup` | 24 | Identifies the row group role. |
| <a id="rowheader"></a>`RowHeader` | 25 | Identifies the row header role. |
| <a id="columnheader"></a>`ColumnHeader` | 26 | Identifies the column header role. |
| <a id="tree"></a>`Tree` | 27 | Identifies the tree role. |
| <a id="treeitem"></a>`TreeItem` | 28 | Identifies the tree item role. |
| <a id="list"></a>`List` | 29 | Identifies the list role. |
| <a id="listitem"></a>`ListItem` | 30 | Identifies the list item role. |
| <a id="listbox"></a>`ListBox` | 31 | Identifies the list box role. |
| <a id="listboxoption"></a>`ListBoxOption` | 32 | Identifies the list box option role. |
| <a id="tabbar"></a>`TabBar` | 33 | Identifies the tab bar role. |
| <a id="tab"></a>`Tab` | 34 | Identifies the tab role. |
| <a id="tabpanel"></a>`TabPanel` | 35 | Identifies the tab panel role. |
| <a id="menubar"></a>`MenuBar` | 36 | Identifies the menu bar role. |
| <a id="menu"></a>`Menu` | 37 | Identifies the menu role. |
| <a id="menuitem"></a>`MenuItem` | 38 | Identifies the menu item role. |
| <a id="menuitemcheckbox"></a>`MenuItemCheckBox` | 39 | Identifies the menu item check box role. |
| <a id="menuitemradio"></a>`MenuItemRadio` | 40 | Identifies the menu item radio role. |
| <a id="image"></a>`Image` | 41 | Identifies the image role. |
| <a id="window"></a>`Window` | 42 | Identifies the window role. |
| <a id="titlebar"></a>`TitleBar` | 43 | Identifies the title bar role. |
| <a id="dialog"></a>`Dialog` | 44 | Identifies the dialog role. |
| <a id="tooltip"></a>`Tooltip` | 45 | Identifies the tooltip role. |
| <a id="region"></a>`Region` | 46 | Identifies the region role. |
| <a id="textrun"></a>`TextRun` | 47 | Identifies the text run role. |

Use `AccessibilityServer.UpdateSetRole(id, AccessibilityServer.AccessibilityRole.Button)` in an active supported display owner. Invalid numeric selectors reject. Element lifetime and native availability follow the owning service.
