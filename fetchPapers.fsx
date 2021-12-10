#r "nuget: FSharp.Data"
open FSharp.Data

let apiKey = "58e12278a2ab0dc7ca70e0713a1cd2c3"

// Run the HTTP web request
let response = Http.RequestString( "https://api.elsevier.com/content/metadata/article", httpMethod = "GET",
    query   = [ "query", "cooling"],
    headers = [ "Accept", "application/json";"X-ELS-APIKey","58e12278a2ab0dc7ca70e0713a1cd2c3"])

let response = Http.RequestString( "https://api.elsevier.com/authenticate", httpMethod = "GET",
    query   = [ "apiKey", apiKey],
    headers = [ "Accept", "application/json";"Authorization","Bearer 58e12278a2ab0dc7ca70e0713a1cd2c3"])
