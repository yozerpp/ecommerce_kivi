// using System.Text;
// using Ecommerce.Entity;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.AspNetCore.Mvc.RazorPages;
//
// namespace Ecommerce.WebImpl.Pages.Shared;
//
// public class UiSessionManager : PageModel
// {
//     public UiSessionManager() { }
//
//     public IActionResult SetQuery() {
//         var s = new SearchSession(){
//             Filters = Filters,
//             Orders = Orders
//         };
//         HttpContext.Session.Set(nameof(SearchSession), SearchSession.Serialize(s));
//         return RedirectToPage("/Index", new QueryString(
//                 string.Join("&&",
//                     string.Join('&', Filters.Select(kv => $"{kv.Key}={kv.Value}")),
//                     Orders.Select(kv => kv.Key + ',' + (kv.Value ? "ASC" : "DESC")))
//             ));
//     }
//
//     [BindProperty] public Dictionary<string, string> Filters { get; set; } = new();
//     [BindProperty] public Dictionary<string, bool> Orders { get; set; } = new();
//
//     public class SearchSession
//     {
//         public static byte[] Serialize(SearchSession session) {
//             var ret = new List<byte>();
//             int c = 0;
//             foreach (var keyValuePair in session.Filters){
//                 if(c++!=0) ret.Add(byte.Parse("\n"));
//                 ret.AddRange(Encoding.UTF8.GetBytes(keyValuePair.Key));
//                 ret.Add(byte.Parse(":"));
//                 ret.AddRange(Encoding.UTF8.GetBytes(keyValuePair.Value));
//             }
//             c = 0;
//             ret.Add(byte.MinValue);
//             foreach (var sessionOrder in session.Orders){
//                 if(c++!=0) ret.Add(byte.Parse("\n"));
//                 ret.AddRange(Encoding.UTF8.GetBytes(sessionOrder.Key));
//                 ret.Add(byte.Parse(":"));
//                 ret.AddRange(Encoding.UTF8.GetBytes(sessionOrder.Value.ToString()));
//             }
//             return ret.ToArray();
//         }
//         public static SearchSession Deserialize(byte[] data) {
//             SearchSession ret = new SearchSession();
//             var buf = new List<byte>();
//             string key = string.Empty;
//             bool stop = false;
//             var it = data.GetEnumerator();
//             while( !stop){
//                 switch (it.Current){
//                     case byte.MinValue:
//                         stop = true;
//                         goto case (byte) '\n';
//                     case (byte)'\n':
//                         ret.Filters[key] = Encoding.UTF8.GetString(buf.ToArray());
//                         break;
//                     case (byte)':':
//                         key = Encoding.UTF8.GetString(buf.ToArray());
//                         break;
//                 }
//             }
//             while(it.MoveNext()){
//                 switch (it.Current){
//                     case (byte)'\n':
//                         ret.Orders[key] = bool.Parse(Encoding.UTF8.GetString(buf.ToArray()));
//                         break;
//                     case (byte)':':
//                         key = Encoding.UTF8.GetString(buf.ToArray());
//                         break;
//                 }
//             }
//             return ret;
//         }
//         public Dictionary<string, string> Filters { get; set; } = new();
//         public Dictionary<string, bool> Orders { get; set; } = new();
//     }
// }