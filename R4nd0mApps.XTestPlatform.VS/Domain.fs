﻿namespace R4nd0mApps.XTestPlatform.Implementation.VS

open Microsoft.VisualStudio.TestPlatform.ObjectModel
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging
open R4nd0mApps.XTestPlatform.Api

// TODO: Split into differnt files
module DataContract = 
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

module Converters = 
    open System
    open System.Collections.Generic
    
    type IRunSettings with
        static member Create() = 
            { new IRunSettings with
                  member __.GetSettings(_ : string) : ISettingsProvider = failwith "Not implemented yet"
                  member __.SettingsXml : string = "<RunSettings/>" }
    
    type IDiscoveryContext with
        static member Create() = 
            { new IDiscoveryContext with
                  member __.RunSettings : IRunSettings = IRunSettings.Create() }
    
    type XTestCase with
        static member Create(x : TestCase) = 
            { TestCase = DataContract.serialize x
              Id = x.Id
              FullyQualifiedName = x.FullyQualifiedName
              DisplayName = x.DisplayName
              Source = x.Source
              CodeFilePath = x.CodeFilePath
              LineNumber = x.LineNumber
              ExtensionUri = x.ExecutorUri }
    
    type ITestCaseDiscoverySink with
        static member Create(x : IXTestCaseDiscoverySink) = 
            { new ITestCaseDiscoverySink with
                  member __.SendTestCase(discoveredTest : TestCase) : unit = 
                      x.SendTestCase(XTestCase.Create discoveredTest) }
    
    type XTestMessageLevel with
        static member Create(x : TestMessageLevel) = 
            match x with
            | TestMessageLevel.Informational -> XTestMessageLevel.Informational
            | TestMessageLevel.Warning -> XTestMessageLevel.Warning
            | TestMessageLevel.Error -> XTestMessageLevel.Error
            | _ -> Prelude.undefined
    
    type IMessageLogger with
        static member Create(x : IXMessageLogger) = 
            { new IMessageLogger with
                  member __.SendMessage(testMessageLevel : TestMessageLevel, message : string) : unit = 
                      x.SendMessage(XTestMessageLevel.Create testMessageLevel, message) }
    
    type IRunContext with
        static member CreateRunContext() = 
            { new IRunContext with
                  member __.GetTestCaseFilter(_ : IEnumerable<string>, _ : Func<string, TestProperty>) : ITestCaseFilterExpression = 
                      null
                  member __.InIsolation : bool = false
                  member __.IsBeingDebugged : bool = false
                  member __.IsDataCollectionEnabled : bool = false
                  member __.KeepAlive : bool = false
                  member __.RunSettings : IRunSettings = IRunSettings.Create()
                  member __.SolutionDirectory : string = null
                  member __.TestRunDirectory : string = null }
    
    type XTestOutcome with
        static member Create = 
            function 
            | TestOutcome.None -> XTestOutcome.None
            | TestOutcome.Passed -> XTestOutcome.Passed
            | TestOutcome.Failed -> XTestOutcome.Failed
            | TestOutcome.Skipped -> XTestOutcome.Skipped
            | TestOutcome.NotFound -> XTestOutcome.NotFound
            | _ -> Prelude.undefined
    
    type XTestResult with
        static member Create(x : TestResult) = 
            { DisplayName = x.DisplayName
              TestCase = XTestCase.Create x.TestCase
              Outcome = XTestOutcome.Create x.Outcome
              ErrorStackTrace = x.ErrorStackTrace
              ErrorMessage = x.ErrorMessage
              EndTime = x.EndTime
              StartTime = x.StartTime }
    
    type IFrameworkHandle with
        static member Create(x : IXTestCaseExecutionSink) = 
            { new IFrameworkHandle with
                  
                  member __.EnableShutdownAfterTestRun 
                      with get () = true : bool
                      and set (_ : bool) = () : unit
                  
                  member __.LaunchProcessWithDebuggerAttached(_ : string, _ : string, _ : string, 
                                                              _ : IDictionary<string, string>) : int = 0
                  member __.RecordAttachments(_ : IList<AttachmentSet>) : unit = ()
                  member __.RecordEnd(_ : TestCase, _ : TestOutcome) : unit = ()
                  
                  member __.RecordResult(testResult : TestResult) : unit = 
                      testResult
                      |> XTestResult.Create
                      |> x.RecordResult
                  
                  member __.RecordStart(_ : TestCase) : unit = ()
                  member __.SendMessage(testMessageLevel : TestMessageLevel, message : string) : unit = 
                      x.SendMessage(XTestMessageLevel.Create testMessageLevel, message) }

open Converters

type internal XTestDiscoverer(obj : obj) = 
    let vstd = obj :?> ITestDiscoverer
    
    let extensionUri = 
        obj.GetType().GetCustomAttributes(true)
        |> Seq.where (fun x -> x :? DefaultExecutorUriAttribute)
        |> Seq.cast<DefaultExecutorUriAttribute>
        |> Seq.head
        |> fun x -> x.ExecutorUri
        |> XExtensionUri
    
    interface IXTestDiscoverer with
        member __.Id : string = obj.GetType().FullName 
        member __.ExtensionUri : XExtensionUri = extensionUri
        member __.DiscoverTests(sources, logger, discoverySink) = 
            vstd.DiscoverTests
                (sources, IDiscoveryContext.Create(), IMessageLogger.Create logger, 
                 ITestCaseDiscoverySink.Create discoverySink)

type internal XTestExecutor(obj : obj) = 
    let vste = obj :?> ITestExecutor
    
    let extensionUri = 
        obj.GetType().GetCustomAttributes(true)
        |> Seq.where (fun x -> x :? ExtensionUriAttribute)
        |> Seq.cast<ExtensionUriAttribute>
        |> Seq.head
        |> fun x -> x.ExtensionUri
        |> XExtensionUri
    
    interface IXTestExecutor with
        member __.Id : string = obj.GetType().FullName 
        member __.ExtensionUri : XExtensionUri = extensionUri
        member __.Cancel() = vste.Cancel()
        member __.RunTests(tests : seq<XTestCase>, executionSink : IXTestCaseExecutionSink) = 
            let tests = tests |> Seq.map (fun x -> x.TestCase |> DataContract.deserialize<TestCase>)
            vste.RunTests(tests, IRunContext.CreateRunContext(), IFrameworkHandle.Create executionSink)