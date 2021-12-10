#r "nuget: FSharp.Data"
open FSharp.Data

let response = HtmlDocument.Load( "https://www.sciencedirect.com/science/article/pii/S2052297521000883")

// Can i create a generic field finder?
let getKeywords (htmlPage:HtmlDocument) = 
    htmlPage.Descendants["div"]
    |> Seq.filter (fun a -> 
        a.TryGetAttribute("class")
        |> Option.map (fun cls -> cls.Value()) = Some "keyword")
    |> Seq.map(fun x -> x.InnerText().ToLower())
    |> Seq.toList

let getMeta (htmlPage:HtmlDocument) = 
    htmlPage.Descendants["meta"]
    |> Seq.map(fun s -> 
        (s.AttributeValue("name"),s.AttributeValue("content")))
    |> Seq.toList