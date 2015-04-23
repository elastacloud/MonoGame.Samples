module B4G.Analytics

open System
open System.Net.Http
open Newtonsoft.Json

type Message =
   { GameId: Guid
     GamerId: Guid
     Message: string 
     LevelScore: int
     LevelCompleteness: double
     LevelTimeRemaining: double }

type OutputMessage =
   { GameId: Guid
     GamerId: Guid
     ShouldMonetize: double }
     
let private CreateReporter id = 
    MailboxProcessor.Start(fun inbox ->
        async {
            while true do
                let! (message:Message) = inbox.Receive()
                System.Diagnostics.Debug.WriteLine (sprintf "Game %A action %s" message.GameId message.Message)
                let wc = new HttpClient()
                try 
                    let task = wc.PostAsync("https://apiapps.azurewebsites.net/api/Game", new StringContent(JsonConvert.SerializeObject(message), System.Text.Encoding.UTF8, "application/json"))
                    let! result = Async.AwaitTask task
                    System.Diagnostics.Debug.WriteLine (sprintf "Game %A action %s result %A" message.GameId message.Message result.StatusCode)
                
                with 
                   | ex -> System.Diagnostics.Debug.WriteLine(ex)
            }
        )

let private agent = CreateReporter 1
let GameBrain message = agent.Post message

let mutable private shouldMonetize = false
let private CreateQueryAgent id = MailboxProcessor.Start(fun inbox ->
         async {
            while true do
                let! (message:(Guid * Guid)) = inbox.Receive()
                let gameId, gamerId = message
                let wc = new HttpClient()
                try 
                    let uri = sprintf "https://apiapps.azurewebsites.net/api/Game/%O/%O" gameId gamerId
                    wc.DefaultRequestHeaders.Accept.ParseAdd "application/json"
                    let task = wc.GetAsync(uri)
                    let! result = Async.AwaitTask task

                    match result.IsSuccessStatusCode with
                    | true -> 
                        let! body = result.Content.ReadAsStringAsync() |> Async.AwaitTask
                        let message:OutputMessage = JsonConvert.DeserializeObject<OutputMessage>(body)
                        shouldMonetize <- (message.ShouldMonetize > 0.)
                    | false -> shouldMonetize <- false
                with 
                    | ex -> 
                        System.Diagnostics.Debug.WriteLine(ex)
                        shouldMonetize <- false
            } 
        )

let private queryAgent = CreateQueryAgent 1

let StartListening (gameId:Guid, gamerId:Guid) =
    async {
        while true do
            do! Async.Sleep 5000
            queryAgent.Post (gameId, gamerId)
    } |> Async.Start

let ShouldMonetize () = 
    shouldMonetize