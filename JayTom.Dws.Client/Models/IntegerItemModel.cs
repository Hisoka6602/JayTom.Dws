using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models
{

    public class IntegerItemModel : BindableBase
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}