﻿namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("Spreads.Collections")>]
[<assembly: AssemblyProductAttribute("Spreads")>]
[<assembly: AssemblyDescriptionAttribute("Spreads")>]
[<assembly: AssemblyVersionAttribute("0.5.0")>]
[<assembly: AssemblyFileVersionAttribute("0.5.0")>]
[<assembly: AssemblyCopyrightAttribute("(c) Victor Baybekov 2015")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.5.0"
