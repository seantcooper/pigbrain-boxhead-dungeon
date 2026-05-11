using System.Linq;
using NUnit.Framework.Internal;
using pigbrain.core.Analysis;
using UnityEngine;
using static System.StringComparison;

namespace pigbrain.core.Analytics
{
    public class SiteLock : ScriptableObject
    {
        public string[] sites;
        public bool isValid => Test();

        bool Test()
        {
            var url = JS.URL;
            return sites.Any(site => url.Contains(site, OrdinalIgnoreCase));
        }
    }
}

// *.crazygames.com
// crazygames.*   // * can be a TLD consisting of 1 or 2 parts like .fr or .com.br

// // Exhaustive list
// www.crazygames.com
// de.crazygames.com
// it.crazygames.com
// vn.crazygames.com
// gr.crazygames.com
// ar.crazygames.com
// th.crazygames.com

// www.crazygames.fr
// www.crazygames.co.id
// www.crazygames.cz
// www.crazygames.dk
// www.crazygames.hu
// www.crazygames.nl
// www.crazygames.no
// www.crazygames.pl
// www.crazygames.com.br
// www.crazygames.ro
// www.crazygames.fi
// www.crazygames.se
// www.crazygames.ru
// www.crazygames.com.ua
// www.crazygames.at
// www.crazygames.jp
// www.crazygames.pt
// www.crazygames.vn
// www.crazygames.com.vn
// www.crazygames.co.kr

// // video ads run on
// games.crazygames.com
