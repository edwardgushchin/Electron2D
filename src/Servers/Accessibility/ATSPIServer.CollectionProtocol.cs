namespace Electron2D;

internal sealed unsafe partial class ATSPIServer
{
    private const string CollectionIntrospection = """
<interface name="org.a11y.atspi.Selection">


    <property name="version" type="u" access="read" />


    <property name="NSelectedChildren" type="i" access="read" />


    <method name="GetSelectedChild">
      <arg direction="in" name="selectedChildIndex" type="i" />
      <arg direction="out" type="(so)" />
      </method>


    <method name="SelectChild">
      <arg direction="in" name="childIndex" type="i" />
      <arg direction="out" type="b" />
    </method>


    <method name="DeselectSelectedChild">
      <arg direction="in" name="selectedChildIndex" type="i" />
      <arg direction="out" type="b" />
    </method>

    <method name="IsChildSelected">
      <arg direction="in" name="childIndex" type="i" />
      <arg direction="out" type="b" />
    </method>


    <method name="SelectAll">
      <arg direction="out" type="b" />
    </method>


    <method name="ClearSelection">
      <arg direction="out" type="b" />
    </method>


    <method name="DeselectChild">
      <arg direction="in" name="childIndex" type="i" />
      <arg direction="out" type="b" />
    </method>

  </interface><interface name="org.a11y.atspi.Table">


    <property name="version" type="u" access="read" />


    <property name="NRows" type="i" access="read" />


    <property name="NColumns" type="i" access="read" />


    <property name="Caption" type="(so)" access="read">
      </property>


    <property name="Summary" type="(so)" access="read">
      </property>


    <property name="NSelectedRows" type="i" access="read" />


    <property name="NSelectedColumns" type="i" access="read" />


    <method name="GetAccessibleAt">
      <arg direction="in" name="row" type="i" />
      <arg direction="in" name="column" type="i" />
      <arg direction="out" type="(so)" />
      </method>


    <method name="GetIndexAt">
      <arg direction="in" name="row" type="i" />
      <arg direction="in" name="column" type="i" />
      <arg direction="out" type="i" />
    </method>


    <method name="GetRowAtIndex">
      <arg direction="in" name="index" type="i" />
      <arg direction="out" type="i" />
    </method>


    <method name="GetColumnAtIndex">
      <arg direction="in" name="index" type="i" />
      <arg direction="out" type="i" />
    </method>


    <method name="GetRowDescription">
      <arg direction="in" name="row" type="i" />
      <arg direction="out" type="s" />
    </method>


    <method name="GetColumnDescription">
      <arg direction="in" name="column" type="i" />
      <arg direction="out" type="s" />
    </method>


    <method name="GetRowExtentAt">
      <arg direction="in" name="row" type="i" />
      <arg direction="in" name="column" type="i" />
      <arg direction="out" type="i" />
    </method>


    <method name="GetColumnExtentAt">
      <arg direction="in" name="row" type="i" />
      <arg direction="in" name="column" type="i" />
      <arg direction="out" type="i" />
    </method>


    <method name="GetRowHeader">
      <arg direction="in" name="row" type="i" />
      <arg direction="out" type="(so)" />
      </method>


    <method name="GetColumnHeader">
      <arg direction="in" name="column" type="i" />
      <arg direction="out" type="(so)" />
      </method>


    <method name="GetSelectedRows">
      <arg direction="out" type="ai" />
      </method>


    <method name="GetSelectedColumns">
      <arg direction="out" type="ai" />
      </method>


    <method name="IsRowSelected">
      <arg direction="in" name="row" type="i" />
      <arg direction="out" type="b" />
    </method>


    <method name="IsColumnSelected">
      <arg direction="in" name="column" type="i" />
      <arg direction="out" type="b" />
    </method>


    <method name="IsSelected">
      <arg direction="in" name="row" type="i" />
      <arg direction="in" name="column" type="i" />
      <arg direction="out" type="b" />
    </method>


    <method name="AddRowSelection">
      <arg direction="in" name="row" type="i" />
      <arg direction="out" type="b" />
    </method>


    <method name="AddColumnSelection">
      <arg direction="in" name="column" type="i" />
      <arg direction="out" type="b" />
    </method>


    <method name="RemoveRowSelection">
      <arg direction="in" name="row" type="i" />
      <arg direction="out" type="b" />
    </method>


    <method name="RemoveColumnSelection">
      <arg direction="in" name="column" type="i" />
      <arg direction="out" type="b" />
    </method>


    <method name="GetRowColumnExtentsAtIndex">
      <arg direction="in" name="index" type="i" />
      <arg direction="out" type="b" />
      <arg direction="out" name="row" type="i" />
      <arg direction="out" name="col" type="i" />
      <arg direction="out" name="row_extents" type="i" />
      <arg direction="out" name="col_extents" type="i" />
      <arg direction="out" name="is_selected" type="b" />
    </method>

  </interface><interface name="org.a11y.atspi.TableCell">


    <property name="version" type="u" access="read" />


    <property access="read" name="ColumnSpan" type="i" />


    <property access="read" name="Position" type="(ii)">
      </property>


    <property access="read" name="RowSpan" type="i" />


    <property access="read" name="Table" type="(so)">
      </property>


    <method name="GetRowColumnSpan">
      <arg direction="out" type="b" />
      <arg direction="out" name="row" type="i" />
      <arg direction="out" name="col" type="i" />
      <arg direction="out" name="row_extents" type="i" />
      <arg direction="out" name="col_extents" type="i" />
    </method>


    <method name="GetColumnHeaderCells">
      <arg direction="out" type="a(so)" />
      </method>


    <method name="GetRowHeaderCells">
      <arg direction="out" type="a(so)" />
      </method>
  </interface>
""";
}
