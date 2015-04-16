module B4G.Analytics

open System.Net
open Newtonsoft.Json

let Reporter id = 
    MailboxProcessor.Start(fun inbox ->
        async {
            while true do
                let! message = inbox.Receive()
                let wc = new WebClient()
                wc.UploadString("http://requestb.in/1jrk9y71", JsonConvert.SerializeObject(message)) |> ignore
            }
        )