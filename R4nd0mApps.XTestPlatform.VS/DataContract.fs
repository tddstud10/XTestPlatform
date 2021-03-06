﻿module R4nd0mApps.XTestPlatform.Implementation.VS.DataContract

open System.IO
open System.Runtime.Serialization
open System.Text

let serialize<'a> (obj : 'a) = 
    let s = DataContractSerializer(typeof<'a>)
    use stream = new MemoryStream()
    s.WriteObject(stream, obj)
    stream.ToArray() |> Encoding.ASCII.GetString

let deserialize<'a> (str : string) = 
    let s = DataContractSerializer(typeof<'a>)
    let bs = Encoding.ASCII.GetBytes str
    use stream = new MemoryStream(bs)
    s.ReadObject(stream) :?> 'a
