module B4G.Analytics

open System
open System.Net.Http
open Newtonsoft.Json

type Message =
   { GameId: Guid
     GamerId: Guid
     Message: string }
     
let private CreateReporter id = 
    MailboxProcessor.Start(fun inbox ->
        async {
            while true do
                let! message = inbox.Receive()
                System.Diagnostics.Debug.WriteLine (sprintf "Game %A action %s" message.GameId message.Message)
                let wc = new HttpClient()
                try 
                    let task = wc.PostAsync("http://requestb.in/1jrk9y71", new StringContent(JsonConvert.SerializeObject(message)))
                    let! result = Async.AwaitTask task
                    System.Diagnostics.Debug.WriteLine (sprintf "Game %A action %s result %A" message.GameId message.Message result.StatusCode)
                
                with 
                   | :? System.Exception as ex -> System.Diagnostics.Debug.WriteLine(ex)
            }
        )

let private agent = CreateReporter 1
let GameBrain message = agent.Post message