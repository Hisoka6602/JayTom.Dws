using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.PluginInterface.Utils {

    public static class Utils {

        public static T? GetVisualChild<T>(DependencyObject parent, Func<T, bool> predicate) where T : Visual {
            var numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < numVisuals; i++) {
                var v = VisualTreeHelper.GetChild(parent, i);
                if (v is not T child) {
                    child = GetVisualChild(v, predicate);
                    if (child is not null) {
                        return child;
                    }
                }
                else {
                    if (predicate(child)) {
                        return child;
                    }
                }
            }

            return null;
        }
    }
}