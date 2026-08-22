namespace Lyt.PhotoPostPro.Model.Library;

[JsonConverter(typeof(JsonStringEnumConverter<DateKind>))]
public enum DateKind
{
    None = 0,
    Captured ,
    Added,
    Edited
}
