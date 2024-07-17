using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection.Metadata;

namespace JayTom.Dws.Camera.Attributes.CameraAttributes {

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    internal sealed class CameraBindableTypeAttribute : Attribute {
        public CameraBindingType CanBindableTypeTypes { get; set; }

        public CameraBindableTypeAttribute(CameraBindingType type) {
            CanBindableTypeTypes = type;
        }
    }
}