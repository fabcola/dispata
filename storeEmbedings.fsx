#r "nuget: FSharp.Data"
#r "nuget: Npgsql.FSharp"
#r "nuget: MathNet.Numerics.FSharp"
#r "C:/Users/COF1SGP/OneDrive - Robert Bosch GmbH/Documents/Visual Studio 2015/Projects/dispata/packages/SQLProvider/lib/net472/FSharp.Data.SqlProvider.dll"
#r "FSharp.Data.SqlProvider.dll"
open FSharp.Data.Sql
open FSharp.Data
open System.IO
open MathNet.Numerics.LinearAlgebra
open Npgsql.FSharp
open System.Data



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

//////////////////// ADD DATA TO MYSQL //////////////////////
/// note that msql does not allow for array data to be stored

let embeddings = File.ReadLines(@"C:\Users\COF1SGP\OneDrive - Robert Bosch GmbH\Downloads\glove.6B\glove.6B.300d.txt")
                        |> Seq.map(fun emb -> emb.Split(' '))
                        |> Seq.map(fun ls -> (ls.[0],String.concat "," ls[1..]))

[<Literal>]
let connectionString1 = "Provider=Microsoft.ACE.OLEDB.12.0; Data Source= @ C:/Users/COF1SGP/OneDrive - Robert Bosch GmbH/Documents/word2vec.accdb"

[<Literal>] 
let dnsConn = @"Driver={Microsoft Access Driver (*.mdb, *.accdb)};Dbq=C:\Users\COF1SGP\OneDrive - Robert Bosch GmbH\Documents\word2vec.accdb"
type db = SqlDataProvider<Common.DatabaseProviderTypes.ODBC, dnsConn>
let ctx = db.GetDataContext()