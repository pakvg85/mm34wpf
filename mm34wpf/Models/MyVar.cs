using System;
using System.Collections.Generic;
using System.Linq;

namespace mm34wpf
{
    public class MyVar
    {
        public string Id { get; set; } = null;
        public string Caption { get; set; } = null;
        public string Prefix { get; set; } = null;
        public string RegexString { get; set; } = null;
        public bool IsPostfix { get; set; } = false;
    }

    public class MyVarList : ObservableCollectionExt<MyVar>
    {
        public IEnumerable<Tuple<string, string>> Columns
        {
            get
            {
                var result = this
                    .Where(x => !x.IsPostfix)
                    .Select(x => new Tuple<string, string>(x.Id, x.Caption));
                return result;
            }
        }
    }
}