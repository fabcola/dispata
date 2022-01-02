#r "nuget: FSharp.Data"
#r "nuget: FSharp.Stats"
#r "nuget: Npgsql.FSharp"
open FSharp.Data
open System.IO
open Npgsql.FSharp
open FSharp.Stats.ML
open FSharp.Stats.ML.Unsupervised
open FSharp.Stats.ML.Unsupervised.HierarchicalClustering

//////////////////////////////////////////////////////////////////

let getWordEncoding (word:string) =
    let connectionString = "Host=localhost; Database=word2vec; Username=postgres; Password=admin;"
    connectionString
    |> Sql.connect
    |> Sql.query (sprintf "SELECT word, encoder FROM public.encodings WHERE word = '%s' LIMIT 1" word)
    //|> Sql.parameters [ "@target_word", Sql.text word]
    |> Sql.execute (fun read -> 
        read.int64Array "encoder")
 
let encoderToArray (encoder:int64[] list) : float[] =
    match encoder with
    | [] -> [|for i in 1..300 -> 0.0|]
    | _ -> (Array.map(fun x -> float(x)/1.0e6) (List.item 0 encoder))
    //|> Array.map(fun x -> float(x)/1.0e6)
    //|> vector 

let a = encoderToArray (getWordEncoding "cooling");;
let b = encoderToArray (getWordEncoding "refrigeration");;

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

let getAllWords (filename:string) = 
    let csv = CsvFile.Load(filename, hasHeaders = true)
    csv.Rows
    |> Seq.map(fun (kw:CsvRow) -> kw.[0].Split(' '))
    |> Seq.concat

let inputFile = "C:/Users/Fabrizio/Documents/Development/fsharp/dispata/dispata/test.csv"

let ignoreWords:Set<string> = set ["and";"for";"nor";"but";"or";"yet";"so";"not";"as";"if";"such";"the";"in";"of";"to";"with";"on";"up";"down";"a";"an";"from";"by";"&"]

// ------------------- A word counter for the csv file -----------------------//
let wordCounter  (singleWords:seq<string>) (targetWord:string)  = 
    let count = Seq.fold(fun (acc:int) (word:string)->  if word.Equals(targetWord) then acc+1 else acc+0) 0 singleWords
    (targetWord,count)

let storeWordCounts (words:seq<string*int>) = 
    let csvTuples = Seq.map(fun (x:string,y:int) -> x+","+string(y)) words
    File.WriteAllLines(@"test_sorted.csv",csvTuples)

let countAllWords (filename:string) = 
    let allWords = (getAllWords filename)
    Seq.distinct(allWords)
    |> Seq.map(wordCounter allWords)
    |> Seq.sortByDescending(fun (x,y)->y)

let sortedWords = countAllWords  inputFile
storeWordCounts sortedWords
// -------------------------- The End ---------------------------------------//

// Should get a dictionary first and a couple methods key-> value and value-> key for later plotting
let getWordEncoderMapping (words:seq<string>) = 
    Seq.map (fun (x:string) -> ((getWordEncoding >> encoderToArray) x,x)) words
    |> Map.ofSeq
   
let EncoderMap = getWordEncoderMapping (getAllWords inputFile)

//let b = encoderToArray (getWordEncoding "refrigeration");;
let allEncoders (enMap:Map<float[],string>) : float[][] = Seq.toArray(Seq.cast(enMap.Keys))

let encoders = allEncoders EncoderMap

let rnd = new System.Random()
let randomInitFactory : IterativeClustering.CentroidsFactory<float []> = 
    IterativeClustering.randomCentroids<float []> rnd

let kmeansResult = 
    IterativeClustering.kmeans <| DistanceMetrics.euclidean <| randomInitFactory 
    <|  encoders <| 30

let wordGroups = 
    let group, _ = Array.unzip (Array.map(fun (x:float[]) -> kmeansResult.Classifier x) encoders)
    let word = Array.map(fun (x:float[]) -> EncoderMap[x]) encoders
    Array.zip group word
                        
let (groupOne:array<int * string>) = Array.filter(fun (g,_) -> g.Equals 4 ) wordGroups   