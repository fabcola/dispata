#r "nuget: FSharp.Data"
#r "nuget: Npgsql.FSharp"
#r "nuget: MathNet.Numerics.FSharp"
open FSharp.Data
open System.IO
open MathNet.Numerics.LinearAlgebra
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

let wordEmbeddings = File.ReadAllLines(@"C:\Users\Fabrizio\Downloads\17\model.txt")
                        |> Seq.map(fun (w:string) -> w.Split(" "))
                        |> Seq.map(convertEmbedding)
                        |> Seq.toList
                        |> List.map(addWord) 
                        
//////////////////////////////////////////////////////////////////

let getWordEncoding (word:string) =
    let connectionString = "Host=localhost; Database=word2vec; Username=postgres; Password=admin;"
    connectionString
    |> Sql.connect
    |> Sql.query (sprintf "SELECT word, encoder FROM public.encodings WHERE word = '%s' LIMIT 1" word)
    //|> Sql.parameters [ "@target_word", Sql.text word]
    |> Sql.execute (fun read -> 
        read.int64Array "encoder")
 
let encoderToArray (encoder:int64[] list) : Vector<float> =
    match encoder with
    | [] -> vector [for i in 1..300 -> 0.0]
    | _ -> vector (Array.map(fun x -> float(x)/1.0e6) (List.item 0 encoder))
    //|> Array.map(fun x -> float(x)/1.0e6)
    //|> vector 

let a = encoderToArray (getWordEncoding "cooling");;
let b = encoderToArray (getWordEncoding "refrigeration");;
let distance = (a-b).L2Norm() 

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

let getJournalIssueArticles (journalIssue:HtmlDocument) = 
    journalIssue.Descendants["a"]
    |> Seq.map (fun a -> 
        a.AttributeValue("href"))
    |> Seq.filter(fun link -> link.Contains("pii"))
    |> Seq.map(fun s -> 
        s.Split("/")
        |> Seq.item 4 )
    |> Seq.distinct

let getJournalIssueKeywords (articleList:HtmlDocument) =
    getJournalIssueArticles articleList
    |> Seq.map(fun art ->
        art
        |> articlePage
        |> getArticleKeywords)
    |> Seq.concat
    |> Seq.toList

// Get all volumes
let getJournalIssue (issue_id:int) = HtmlDocument.Load("https://www.sciencedirect.com/journal/applied-energy/vol/"+string(issue_id)+"/suppl/C")

let saveKeywords (keywords:string list) = File.WriteAllLines(@"test.csv",keywords)

let getAllJournalKeywords (journalName:string, lastIssueId:int) = 
    [303 .. 308]
    |> List.map(getJournalIssue)
    |> List.map(getJournalIssueKeywords)
    |> List.concat
    |> saveKeywords


// A word counter for the csv file
let wordCounter  (singleWords:seq<string>) (targetWord:string)  = 
    let count = Seq.fold(fun (acc:int) (word:string)->  if word.Equals(targetWord) then acc+1 else acc+0) 0 singleWords
    (targetWord,count)

let getAllWords (filename:string) = 
    let csv = CsvFile.Load(filename, hasHeaders = true)
    csv.Rows
    |> Seq.map(fun (kw:CsvRow) -> kw.[0].Split(' '))
    |> Seq.concat

let countAllWords (filename:string) = 
    let allWords = (getAllWords filename)
    Seq.distinct(allWords)
    |> Seq.map(wordCounter allWords)

// Still need to sort -> then use the embeddings for clustering