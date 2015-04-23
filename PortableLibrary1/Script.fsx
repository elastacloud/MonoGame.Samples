// Learn more about F# at http://fsharp.net. See the 'F# Tutorial' project
// for more guidance on F# programming.


    async {
        while true do
            do! Async.Sleep 2000
            printfn "foo %A" System.DateTime.Now
    } |> Async.Start
