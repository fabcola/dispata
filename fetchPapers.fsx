#r "nuget: FSharp.Data"
open FSharp.Data

let apiKey = "58e12278a2ab0dc7ca70e0713a1cd2c3"

// Run the HTTP web request
let response2 = Http.RequestString( "https://api.elsevier.com/content/article/entitlement", httpMethod = "GET",
    query   = [ "apiKey", apiKey;"doi", "10.1016/j.arabjc.2017.05.011"],
    headers = [ "Accept", "application/json";"X-ELS-APIKey","58e12278a2ab0dc7ca70e0713a1cd2c3"])

let response = HtmlDocument.Load( "https://www.sciencedirect.com/science/article/pii/S2052297521000883")

let keywords (res:HtmlDocument) = 
    res.Descendants["div"]
    |> Seq.filter (fun a -> 
        a.TryGetAttribute("class")
        |> Option.map (fun cls -> cls.Value()) = Some "keyword")
    |> Seq.map(fun x -> x.InnerText().ToLower())
    |> Seq.toList