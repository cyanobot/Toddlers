using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static Toddlers.HARCompat;

namespace Toddlers
{ 
    //wrapper class to go around extended graphics to allow me to manipulate them more easily 

    public class HARExtendedGraphic
    {
        public static FieldInfo f_path;
        public static FieldInfo f_paths;
        public static FieldInfo f_extendedGraphics;
        public static FieldInfo f_conditions;

        public object original;
        public Type origType;
        public HARExtendedGraphic parent;

        string nameAsField;
        public bool isConditional;

        string path;
        List<string> paths;
        public List<object> extendedGraphics;
        public List<object> conditions;

        public HARExtendedGraphic(object original, HARExtendedGraphic parent, string nameAsField = "")
        {
            this.original = original;
            origType = original.GetType();
            this.parent = parent;

            this.nameAsField = nameAsField;

            if (!t_AbstractExtendedGraphic.IsAssignableFrom(origType))
            {
                throw new ArgumentException("Tried to create HARExtendedGraphic wrapper from an object that is not an HAR extended graphic");
            }
            
            isConditional = t_ExtendedConditionGraphic.IsAssignableFrom(origType);
            if (isConditional)
            {
                conditions = (f_conditions.GetValue(original) as IList).Cast<object>().ToList();
            }
            else
            {
                conditions = null;
            }

            path = f_path.GetValue(original) as string;
            paths = f_path.GetValue(original) as List<string>;
            extendedGraphics = (f_extendedGraphics.GetValue(original) as IList).Cast<object>().ToList();
            /*
            LogUtil.DebugLog($"obj_extendedGraphics: {obj_extendedGraphics}, " +
                $"ilist_extendedGraphics: {ilist_extendedGraphics}, " +
                $"extendedGraphics: {extendedGraphics}, " +
                $"Count: {extendedGraphics?.Count}");
            */
        }


        public string ShortenedPath(string path)
        {
            if (path.Length <= 10) return path;
            string shortPath = path.Split('/').Last();
            if (shortPath.Length > 0) return shortPath;
            return path;
        }

        public string ShortDescription()
        {
            if (!nameAsField.NullOrEmpty()) return nameAsField;
            if (!path.NullOrEmpty())
            {
                return ShortenedPath(path);
            }
            if (!paths.NullOrEmpty())
            {
                return ShortenedPath(paths[0]);
            }
            else return origType.Name;
        }


        public string ShortDescriptionWithParent()
        {
            if (parent == null) return ShortDescription();
            else return parent.ShortDescription() + "." + ShortDescription();
        }

        public string Description()
        {
            StringBuilder sb = new StringBuilder();

            //field name and type
            if (!nameAsField.NullOrEmpty())
            {
                sb.AppendLine(nameAsField + " - " + origType.Name);
            }
            else
            {
                sb.AppendLine(origType.Name);
            }

            //parent
            if (parent == null)
            {
                sb.AppendLine("top level graphic");
            }
            else
            {
                sb.AppendLine("child of: " + parent.ShortDescriptionWithParent());
            }

            //graphic path
            if (!path.NullOrEmpty())
            {
                sb.AppendLine($"path: {path}");
            }
            else if (!paths.NullOrEmpty())
            {
                sb.AppendLine($"paths ({paths.Count}): {path[0]} ...");
            }
            else
            {
                sb.AppendLine("no path");
            }

            //children
            if (!extendedGraphics.NullOrEmpty())
            {
                sb.AppendLine($"children ({extendedGraphics.Count}): {extendedGraphics[0]} ...");
            }
            else
            {
                sb.AppendLine("no children");
            }

            //conditions
            if (isConditional)
            {
                if (!conditions.NullOrEmpty())
                {
                    sb.AppendLine($"{conditions.Count} conditions, of which " +
                        $"{conditions.Count(c => t_ConditionAge.IsAssignableFrom(c.GetType()))}" +
                        $" are ConditionAge");
                }
                else
                {
                    sb.AppendLine("no conditions");
                }
            }

            return sb.ToString();
        }
    }
}
