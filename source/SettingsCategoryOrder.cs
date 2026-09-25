using System;using System.Linq;using System.Collections.Generic;
namespace KianaPet {
 public static class SettingsCategoryOrder {
  public static readonly string[] Recommended={"常用","外观与互动","音乐","通知中心","活动与作息","免打扰","体验","ChatGPT 联动","启动与联动","快捷键","无障碍","效果预览","最近调整","场景与维护","使用说明"};
  public static string[] Normalize(string[] saved){var known=(saved??new string[0]).Where(s=>Recommended.Contains(s)).Distinct(StringComparer.Ordinal).ToArray();return known.Length==0?new string[0]:known.Concat(Recommended).Distinct(StringComparer.Ordinal).ToArray();}
  public static string[] Resolve(string[] saved,IEnumerable<string> available){var names=available.Distinct(StringComparer.Ordinal).ToArray();return Normalize(saved).Concat(Recommended).Concat(names).Where(names.Contains).Distinct(StringComparer.Ordinal).ToArray();}
  public static string[] Move(string[] order,string name,string target,bool after){var items=order.ToList();if(name==target||!items.Contains(name)||!items.Contains(target))return order.ToArray();items.Remove(name);items.Insert(items.IndexOf(target)+(after?1:0),name);return items.ToArray();}
 }
}
