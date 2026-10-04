namespace Electron2D;

public partial class LineEdit
{
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        foreach (var property in LineEditProperties) yield return property;
    }
    private static readonly PropertyDescriptor[] LineEditProperties =
    [
        new PropertyDescriptor<LineEdit,bool>(nameof(DrawControlChars),c=>c.DrawControlChars,(c,v)=>c.DrawControlChars=v,_=>false,stored:true),
        new PropertyDescriptor<LineEdit,HorizontalAlignment>(nameof(Alignment), c => c.Alignment, (c,v) => c.Alignment=v, _ => HorizontalAlignment.Left, stored:true),
        new PropertyDescriptor<LineEdit,string>(nameof(PlaceholderText), c => c.PlaceholderText, (c,v) => c.PlaceholderText=v, _ => "", stored:true),
        new PropertyDescriptor<LineEdit,bool>(nameof(Secret), c => c.Secret, (c,v) => c.Secret=v, _ => false, stored:true),
        new PropertyDescriptor<LineEdit,bool>(nameof(CaretBlink), c => c.CaretBlink, (c,v) => c.CaretBlink=v, _ => false, stored:true),
        new PropertyDescriptor<LineEdit,bool>(nameof(CaretForceDisplayed), c => c.CaretForceDisplayed, (c,v) => c.CaretForceDisplayed=v, _ => false, stored:true),
        new PropertyDescriptor<LineEdit,bool>(nameof(CaretMidGrapheme), c => c.CaretMidGrapheme, (c,v) => c.CaretMidGrapheme=v, _ => false, stored:true),
        new PropertyDescriptor<LineEdit,bool>(nameof(BackspaceDeletesCompositeCharacterEnabled), c => c.BackspaceDeletesCompositeCharacterEnabled, (c,v) => c.BackspaceDeletesCompositeCharacterEnabled=v, _ => false, stored:true),
        new PropertyDescriptor<LineEdit,bool>(nameof(ClearButtonEnabled), c => c.ClearButtonEnabled, (c,v) => c.ClearButtonEnabled=v, _ => false, stored:true),
        new PropertyDescriptor<LineEdit,bool>(nameof(DeselectOnFocusLossEnabled), c => c.DeselectOnFocusLossEnabled, (c,v) => c.DeselectOnFocusLossEnabled=v, _ => true, stored:true),
        new PropertyDescriptor<LineEdit,bool>(nameof(DragAndDropSelectionEnabled), c => c.DragAndDropSelectionEnabled, (c,v) => c.DragAndDropSelectionEnabled=v, _ => true, stored:true),
        new PropertyDescriptor<LineEdit,bool>(nameof(ExpandToTextLength), c => c.ExpandToTextLength, (c,v) => c.ExpandToTextLength=v, _ => false, stored:true),
        new PropertyDescriptor<LineEdit,bool>(nameof(Flat), c => c.Flat, (c,v) => c.Flat=v, _ => false, stored:true),
        new PropertyDescriptor<LineEdit,bool>(nameof(KeepEditingOnTextSubmit), c => c.KeepEditingOnTextSubmit, (c,v) => c.KeepEditingOnTextSubmit=v, _ => false, stored:true),
        new PropertyDescriptor<LineEdit,bool>(nameof(MiddleMousePasteEnabled), c => c.MiddleMousePasteEnabled, (c,v) => c.MiddleMousePasteEnabled=v, _ => true, stored:true),
        new PropertyDescriptor<LineEdit,bool>(nameof(SelectAllOnFocus), c => c.SelectAllOnFocus, (c,v) => c.SelectAllOnFocus=v, _ => false, stored:true),
        new PropertyDescriptor<LineEdit,bool>(nameof(ShortcutKeysEnabled), c => c.ShortcutKeysEnabled, (c,v) => c.ShortcutKeysEnabled=v, _ => true, stored:true),
        new PropertyDescriptor<LineEdit,bool>(nameof(Editable), c => c.Editable, (c,v) => c.Editable=v, _ => true, stored:true),
        new PropertyDescriptor<LineEdit,bool>(nameof(SelectingEnabled), c => c.SelectingEnabled, (c,v) => c.SelectingEnabled=v, _ => true, stored:true),
        new PropertyDescriptor<LineEdit,string>(nameof(SecretCharacter), c => c.SecretCharacter, (c,v) => c.SecretCharacter=v, _ => "•", stored:true),
        new PropertyDescriptor<LineEdit,TextDirection>(nameof(TextDirection), c => c.TextDirection, (c,v) => c.TextDirection=v, _ => global::Electron2D.TextDirection.Auto, stored:true),
        new PropertyDescriptor<LineEdit,StructuredTextParser>(nameof(StructuredTextBIDIOverride), c => c.StructuredTextBIDIOverride, (c,v) => c.StructuredTextBIDIOverride=v, _ => StructuredTextParser.Default, stored:true),
        new PropertyDescriptor<LineEdit,string[]>(nameof(StructuredTextBIDIOverrideOptions), c => c.StructuredTextBIDIOverrideOptions, (c,v) => c.StructuredTextBIDIOverrideOptions=v, _ => Array.Empty<string>(), stored:true),
        new PropertyDescriptor<LineEdit,string>(nameof(Language), c => c.Language, (c,v) => c.Language=v, _ => "", stored:true),
        new PropertyDescriptor<LineEdit,double>(nameof(CaretBlinkInterval), c => c.CaretBlinkInterval, (c,v) => c.CaretBlinkInterval=v, _ => .65, stored:true),
        new PropertyDescriptor<LineEdit,Texture?>(nameof(RightIcon), c => c.RightIcon, (c,v) => c.RightIcon=v, _ => null, stored:true),
        new PropertyDescriptor<LineEdit,float>(nameof(RightIconScale), c => c.RightIconScale, (c,v) => c.RightIconScale=v, _ => 1, stored:true),
        new PropertyDescriptor<LineEdit,LineEditIconExpandMode>(nameof(IconExpandMode), c => c.IconExpandMode, (c,v) => c.IconExpandMode=v, _ => LineEditIconExpandMode.OriginalSize, stored:true),
        new PropertyDescriptor<LineEdit,int>(nameof(MaxLength), c => c.MaxLength, (c,v) => c.MaxLength=v, _ => 0, stored:true),
        new PropertyDescriptor<LineEdit,string>(nameof(Text), c => c.Text, (c,v) => c.Text=v, _ => "", stored:true),
        new PropertyDescriptor<LineEdit,int>(nameof(CaretColumn), c => c.CaretColumn, (c,v) => c.CaretColumn=v, _ => 0, stored:true),
    ];
}
