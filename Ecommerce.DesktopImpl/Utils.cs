using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Ecommerce.Dao.Default;
using Ecommerce.Entity;
using Ecommerce.Entity.Common.Meta;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Identity.Client;
using Ninject;

namespace Ecommerce.DesktopImpl
{
    internal static class Utils
    {
        private static readonly Dictionary<Type, ICollection<string>> ColumnNamesCache = new();
        private static IModel Model = Program.Kernel.Get<DefaultDbContext>().Model;
        public static void Error(string message)
        {
            MessageBox.Show(message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static void Info(string message)
        {
            MessageBox.Show(message,"Bilgilendirme", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private static IEntityType? GetEntityType(Type clrType)
        {
            return Model.FindEntityType(clrType) ?? (clrType.BaseType!=null?Model.FindEntityType(clrType.BaseType): null);
        }

        public static object DictToObject(Type type,ICollection<KeyValuePair<string, string>> dictionary) {
            var ret =type.GetConstructor([]).Invoke([]);
            Dictionary<string, Dictionary<string, string>> complexProps = new();
            foreach (var kv in dictionary){
                if (kv.Key.Contains('_')){
                    var s = kv.Key.Split('_');
                    (complexProps.GetValueOrDefault(s[0]) ?? (complexProps[s[0]] = new Dictionary<string, string>()))
                        [string.Join('_', s.Skip(1))] = kv.Value;
                    continue;
                }

                var prop = type.GetProperty(kv.Key);
                prop.SetValue(ret, ParseProp(prop.PropertyType,kv.Value));
            }
            foreach (var complexProp in complexProps){
                var p = type.GetProperty(complexProp.Key);
                p.SetValue(ret,DictToObject(p.PropertyType, complexProp.Value));
            }
            return ret;
        }

        public static object ParseProp(Type tp, string val) {
            var pars = tp.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, [typeof(string)]);
            if (pars!=null )   
                return pars.Invoke(null, [val]);
            if (tp.IsEnum)
                return Enum.Parse(tp, val);
            if (tp.IsArray){
                var elems = val.Split(',');
                var t = tp.GetElementType();
                var a = Array.CreateInstance(t, elems.Length);
                for (int i = 0; i < elems.Length; i++){
                    a.SetValue(ParseProp(t, elems[i]), i);
                }

                return a;
            }
            if (typeof(string).IsAssignableFrom(tp)) return val;
            else throw new ArgumentException("Type deserialization not supported: " + tp.FullName);
        }
        public static ICollection<string> ColumnNames(Type entityType, string[] exclude, string[] include, bool onlyDeclared = false)
        {
            var t = GetEntityType(entityType);
            IEnumerable<PropertyInfo> properties = onlyDeclared?entityType.GetProperties().Where(p=>p.DeclaringType==entityType):entityType.GetProperties();
            if (t != null)
                properties = properties.Where(p1 => {
                    var p2 = t.FindProperty(p1.Name);
                    return p2==null && t.FindNavigation(p1.Name)==null  && t.FindComplexProperty(p1.Name)==null || //property from base class.
                           p2!=null&& !exclude.Contains(p1.Name) && !p2.IsShadowProperty() &&
                           (include.Contains(p1.Name)||!p2.IsKey() && !p2.IsForeignKey() && !p2.IsPrimaryKey());
                });
            properties = properties.Where(p => !exclude.Contains(p.Name));
            var ret=ColumnNamesProperties(properties);
            if (t != null){
                var nestedIncludes = NestIncludes(include);
                var nestedExcludes = NestIncludes(exclude);
                foreach (var navigation in t.GetNavigations()
                             .Where(n => (!onlyDeclared || n.DeclaringEntityType.ClrType == entityType)&&!n.IsCollection && n.IsOnDependent && !n.IsShadowProperty()).Where(n=>!exclude.Contains(n.Name))){
                    if(!include.Any(i => i.Split('_').First().Equals(navigation.Name) || 
                                         i.Split('_').First().Equals(navigation.Name))) continue;
                    ret.AddRange(ColumnNames(navigation.TargetEntityType.ClrType,nestedExcludes,nestedIncludes).Where(s=>nestedIncludes.Contains(s)).Select(s=>$"{navigation.Name}_{s}"));
                }
                ret.AddRange(ColumnNamesComplex(t.GetComplexProperties()));
            }
            return ret;
        }

        private static string[] NestIncludes(params string[] exclude) {
            return exclude.Select(s => string.Join('_',s.Split('_').Skip(1))).ToArray();
        }
        private static ICollection<string>ColumnNamesComplex(IEnumerable<IComplexProperty> complexProperties) {
            var ret = new List<string>();
            foreach (var complexProperty in complexProperties){
                ret.AddRange(ColumnNamesProperties(complexProperty.ComplexType.GetProperties().Select(c=>c.PropertyInfo!)).Select(s=>$"{complexProperty.Name}_{s}"));
            }
            return ret;
        }
        private static List<string> ColumnNamesProperties(IEnumerable<PropertyInfo> properties) {
            var ret = new List<string>();
            foreach (var prop in properties)
            {
                switch (Type.GetTypeCode(prop.PropertyType))
                {
                    case TypeCode.Boolean:
                    case TypeCode.Char:
                    case TypeCode.Byte:
                    case TypeCode.DateTime:
                    case TypeCode.String:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                    case TypeCode.Decimal:
                    case TypeCode.Double:
                    case TypeCode.Single:
                    case TypeCode.Object: //
                        ret.Add(prop.Name);
                        break;
                        //ret.AddRange(ColumnNames(prop.PropertyType));
                        break;
                }
            }
            return ret;
        }
        public static object? GetInput(Type type, string cap, string[]? include = null,params string[] ignore){
            Form prompt = new Form()
            {
                Width = 250,
                Height = 300,
                Text = cap,
                StartPosition = FormStartPosition.CenterScreen,
            };
            var container1 = new FlowLayoutPanel(){
                FlowDirection = FlowDirection.TopDown, Dock = DockStyle.Fill,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            Button ok = new Button() { Text = "OK", Left = 280, Width = 80, Top = 80, Dock = DockStyle.Bottom, DialogResult = DialogResult.OK };
            Dictionary<PropertyInfo, TextBox> inputs = new();
            var t = Model.FindEntityType(type);
            ICollection<PropertyInfo> fileProperties = new List<PropertyInfo>();
            foreach (var propertyInfo in type.GetProperties().Where(p=> {
                         var p2 = t?.FindProperty(p.Name);
                         return t == null || p2 != null && ((!p2.IsForeignKey() && !p2.IsKey()) ||
                                                             (include?.Contains(p2.Name) ?? false));
                     })){
                if(ignore.Contains(propertyInfo.Name)) continue;
                if (propertyInfo.GetCustomAttribute<ImageAttribute>() != null){
                    fileProperties.Add(propertyInfo);
                    continue;
                }
                var container = new FlowLayoutPanel{
                    FlowDirection = FlowDirection.LeftToRight,AutoSize = true,Dock = DockStyle.Top, Anchor = AnchorStyles.Top | AnchorStyles.Left,
                };
                 foreach (var valueTuple in CreateTextBox(propertyInfo)){
                    container.Controls.Add(valueTuple.label);
                    container.Controls.Add(valueTuple.inputBox);
                    inputs.Add(propertyInfo, valueTuple.inputBox);
                 }
                 container1.Controls.Add(container);
            }
            container1.Controls.Add(ok);
            prompt.Controls.Add(container1);
            prompt.AcceptButton = ok;
            var resp = prompt.ShowDialog();
            if (resp == DialogResult.Abort || resp == DialogResult.Cancel) return default;
            var ret = type.GetConstructor([]).Invoke([]);
            foreach (var fileProperty in fileProperties){
                var file = PromptFile(fileProperty.Name);
                if(file==null) continue;
                if(fileProperty.PropertyType ==typeof(string) )
                    fileProperty.SetValue(ret, Convert.ToBase64String(file));
                else fileProperty.SetValue(ret, file);
            }
            foreach (var kv in inputs){
                kv.Key.SetValue(ret, ParseProp(kv.Key.PropertyType, kv.Value.Text));
            }
            return ret;
        }

        private static byte[]? PromptFile(string cap) {
            using OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = "c:\\";
            openFileDialog.Filter = "Image files (*.jpg, *.png)|*.jpg; *.png|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.Title = "Select a file for " + cap;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Get the selected file path
                string filePath = openFileDialog.FileName;

                // Do something with the file (e.g., read)
                return File.ReadAllBytes(filePath);
            }

            return null;
        }
        private static IEnumerable<(Label label, TextBox inputBox)> CreateTextBox(PropertyInfo propertyInfo) {
            var c =Type.GetTypeCode(propertyInfo.PropertyType);
            if (c == TypeCode.Object  && Model.FindEntityType(propertyInfo.DeclaringType)?.FindComplexProperty(propertyInfo.Name)!=null ){ 
                return propertyInfo.PropertyType.GetProperties().SelectMany(CreateTextBox );
            }
            var label = new Label{
                Text = propertyInfo.Name,
                Left = 20, Top = 20, AutoSize = true,Anchor = AnchorStyles.Top | AnchorStyles.Left,
                Dock = DockStyle.Left
            };
            var inputBox = new TextBox{
                PlaceholderText  = propertyInfo.Name,AutoSize = true,
                Dock = DockStyle.Fill, Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            return [(label, inputBox)];
        }

        public static ICollection<(string, object)> ToPairs(object entity, string[] exclude, string[] include)
{
    var vals = new List<(string, object)>();
    var t = GetEntityType(entity.GetType());
    IEnumerable<PropertyInfo> properties = entity.GetType().GetProperties();
    if (t != null)
        properties = properties.Where(p1 => {
            var p2 = t.FindProperty(p1.Name);
            return p2 == null && t.FindNavigation(p1.Name) == null &&
                   t.FindComplexProperty(p1.Name) == null && !exclude.Contains(p1.Name)|| //property from base class.
                   p2 != null && !exclude.Contains(p1.Name) && !p2.IsShadowProperty() &&
                   (include.Contains(p1.Name) || !p2.IsKey() && !p2.IsForeignKey() && !p2.IsPrimaryKey());
        });
    vals.AddRange(ToPairsProperties(entity, properties));
    if (t != null){
        var nestedExcludes = NestIncludes(exclude);
        var nestedIncludes = NestIncludes(include);
        foreach (var navigation in t.GetNavigations()
                     .Where(n => !n.IsCollection && n.IsOnDependent && !n.IsShadowProperty()).Where(n=>!exclude.Contains(n.Name))){
            object? n = navigation.PropertyInfo.GetValue(entity);
            if (n==null) continue;
            if(!include.Any(i => i.Split('_').First().Equals(navigation.Name) || 
                                 i.Split('_').First().Equals(navigation.Name))) continue;
            vals.AddRange(ToPairs(n, nestedExcludes, nestedIncludes).Where(kv=>nestedIncludes.Contains(kv.Item1)).Select(s=>($"{navigation.Name}_{s.Item1}", s.Item2)));
        }
        vals.AddRange(ComplexToPairs(entity, t.GetComplexProperties()));
        
    }
    return vals;
}

private static ICollection<(string, object)> ToPairsProperties(object entity, IEnumerable<PropertyInfo> properties)
{
    var result = new List<(string, object)>();
    foreach (var prop in properties)
    {
        var name = prop.Name;
        var value = prop.GetValue(entity);

        switch (Type.GetTypeCode(prop.PropertyType))
        {
            case TypeCode.Boolean:
            case TypeCode.Char:
            case TypeCode.Byte:
            case TypeCode.DateTime:
            case TypeCode.String:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
            case TypeCode.Decimal:
            case TypeCode.Double:
            case TypeCode.Single:
            case TypeCode.Object:
                result.Add((name, value?.ToString() ?? "null"));
                break;

            default:
                result.Add((name, "null"));
                break;
        }
    }
    return result;
}

    private static ICollection<(string, object)> ComplexToPairs(object entity, IEnumerable<IComplexProperty> complexProperties)
    {
        var result = new List<(string, object)>();
        foreach (var complexProperty in complexProperties)
        {
            var complexValue = complexProperty.PropertyInfo?.GetValue(entity);
            var prefix = complexProperty.Name;
            if (complexValue == null)
            {
                foreach (var prop in complexProperty.ComplexType.GetProperties())
                    result.Add(($"{prefix}_{prop.Name}", "null"));
            }
            else
            {
                var nestedPairs = ToPairsProperties(complexValue, complexProperty.ComplexType.GetProperties().Select(p => p.PropertyInfo!));
                foreach (var (name, val) in nestedPairs)
                {
                    result.Add(($"{prefix}_{name}", val));
                }
            }
        }   
        return result;
    }        
    public static ICollection<object> ToRow(object entity, params string[] exclude)
        {
            var vals = new List<object>();
            var t = GetEntityType(entity.GetType());
            IEnumerable<PropertyInfo> properties = entity.GetType().GetProperties();
            if (t != null)
                properties = properties.Where(p1 => {
                    var p2 = t.FindProperty(p1.Name);
                    return p2==null && t.FindComplexProperty(p1.Name)==null || !exclude.Contains(p1.Name) && !p2.IsShadowProperty() && !p2.IsKey() && !p2.IsForeignKey() && !p2.IsPrimaryKey();
                });
            vals.AddRange(ToRowProperties(entity, properties));
            if (t != null){
                var nestedExcludes = NestIncludes(exclude);
                foreach (var navigation in t.GetNavigations()
                             .Where(n => !n.IsCollection && n.IsOnDependent && !n.IsShadowProperty()).Where(n=>!exclude.Contains(n.Name))){
                    object? n = navigation.PropertyInfo.GetValue(entity);
                    if (n==null) continue;
                    vals.AddRange(ToRow(n, nestedExcludes));
                }
                vals.AddRange(ComplexToRow(entity, t.GetComplexProperties()));
            }
            return vals;
        }
        private static ICollection<object> ComplexToRow(object entity, IEnumerable<IComplexProperty> properties) {
            var vals = new List<object>();
            foreach (var complexProperty in properties)
            {
                var f = complexProperty.Name;
                var c = complexProperty.PropertyInfo!.GetValue(entity);
                if (c == null){
                    var cProperties = complexProperty.ComplexType.GetProperties();
                    var r = new List<object>();
                    for (int i = 0; i < cProperties.Count(); i++){
                        r.Add("null");
                    } 
                    return r;
                }
                vals.AddRange(ToRowProperties(c, complexProperty.ComplexType.GetProperties().Select(ct=>ct.PropertyInfo!)));
            }
            return vals;
        }
        private static List<object> ToRowProperties(object entity, IEnumerable<PropertyInfo> properties) {
            var ret =new List<object>();
            foreach (var prop in properties)
            {
                var f = prop.Name;
                switch (Type.GetTypeCode(prop.PropertyType))
                {
                    case TypeCode.Boolean:
                    case TypeCode.Char:
                    case TypeCode.Byte:
                    case TypeCode.DateTime:
                    case TypeCode.String:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                    case TypeCode.Decimal:
                    case TypeCode.Double:
                    case TypeCode.Single:
                    case TypeCode.Object: //
                        ret.Add(prop.GetValue(entity)?.ToString()??"null");
                        break;
                    default:
                        ret.Add((f, "null"));
                        break;
                }
            }
            return ret;
        }

        public static FlowLayoutPanel InitDetailsScheme(Dictionary<string, TextBox> textBoxes, Type type, string[] exclude, string[] include, bool onlyDeclared = false) {
            var infoContainer = new FlowLayoutPanel(){
                Dock = DockStyle.Fill, WrapContents = true, FlowDirection = FlowDirection.TopDown,AutoScroll = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left, Visible = true, Enabled = true
            };
            foreach (var kv in Utils.ColumnNames(type, exclude, include, onlyDeclared:onlyDeclared)){
                if(kv.Equals(nameof(User.PasswordHash))) continue;
                var label = new Label{
                    Text = kv, TextAlign = ContentAlignment.MiddleLeft, AutoSize = true,
                    Font = new Font(FontFamily.GenericSansSerif, 9, FontStyle.Regular)
                };
                var text = new TextBox{
                    Text = kv, ReadOnly = true, AutoSize = true,
                    Font = new Font(FontFamily.GenericSansSerif, 9, FontStyle.Regular), Enabled = true, Visible = true
                };
                var container = new FlowLayoutPanel{
                    FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Dock = DockStyle.None,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left, Visible = true, Enabled = true
                };
                textBoxes.Add( kv,text);
                container.Controls.Add(label);
                container.Controls.Add(text);
                infoContainer.Controls.Add(container);
            }
            return infoContainer;
        }
    }
}
