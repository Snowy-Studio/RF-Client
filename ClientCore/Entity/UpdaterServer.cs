﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientCore.Entity
{
    public readonly record struct UpdaterServer(
        int? id,
        string name,
        int type,
        string location,
        string url
    );
}
