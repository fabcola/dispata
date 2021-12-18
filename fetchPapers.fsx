#r "nuget: FSharp.Data"
#r "nuget: Npgsql.FSharp"

open FSharp.Data
open System.IO

open Npgsql.FSharp

//////////////////// ADD DATA to POSTGRESQL /////////////////////
let addWord (word:string, vector:int64[]) : int =
    let connectionString = "Host=localhost; Database=word2vec; Username=postgres; Password=admin;"
    connectionString
    |> Sql.connect
    |> Sql.query "INSERT INTO public.encodings (word, encoder) VALUES (@word, @vector)"
    |> Sql.parameters [ "@word", Sql.text word; "@vector", Sql.int64Array vector ]
    |> Sql.executeNonQuery

let convertEmbedding (word:string[]) = 
    (word.[0], Array.map(fun x -> int64((float x)*1.0e6)) word.[1..])

let wordEmbeddings = File.ReadLines(@"C:\Users\Fabrizio\Downloads\17\model.txt")
                        |> Seq.map(fun (w:string) -> w.Split(" "))
                        |> Seq.map(convertEmbedding)
                        |> Seq.map(addWord) 
//////////////////////////////////////////////////////////////////
/// 
/// 
let articlePage (pii:string) = match pii.Length with 
                                | 17 -> HtmlDocument.Load("https://www.sciencedirect.com/science/article/pii/"+pii)
                                | _ -> failwith "Incorrect article pii"

let journalIssue = HtmlDocument.Load("https://www.sciencedirect.com/journal/applied-energy/vol/307/suppl/C")
// Can i create a generic field finder?
let getArticleKeywords (articlePage:HtmlDocument) = 
    articlePage.Descendants["div"]
    |> Seq.filter (fun a -> 
        a.TryGetAttribute("class")
        |> Option.map (fun cls -> cls.Value()) = Some "keyword")
    |> Seq.map(fun x -> x.InnerText().ToLower())

let getArticleMeta (articlePage:HtmlDocument) = 
    articlePage.Descendants["meta"]
    |> Seq.map(fun s -> 
        (s.AttributeValue("name"),s.AttributeValue("content")))

let getJournalIssueLinks (journalIssue:HtmlDocument) = 
    journalIssue.Descendants["a"]
    |> Seq.map (fun a -> 
        a.AttributeValue("href"))
    |> Seq.filter(fun link -> link.Contains("pii"))
    |> Seq.map(fun s -> 
        s.Split("/")
        |> Seq.item 4 )
    |> Seq.distinct

let getJournalIssueKeywords (articleList:HtmlDocument) =
    getJournalIssueLinks articleList
    |> Seq.map(fun art ->
        art
        |> articlePage
        |> getArticleKeywords)
    |> Seq.concat
    |> Seq.toList

