﻿namespace Spreads.Tests.Collections.Benchmarks

open FsUnit
open NUnit.Framework

open System
open System.Collections.Generic
open System.Diagnostics
open Spreads
open Spreads.Collections
open System.Threading
open System.Threading.Tasks

open Deedle

/// Insert last (most common usage), first (worst case usage) and read (forward and backward)
/// for each collection
module CollectionsBenchmarks =
  
  /// run f and measure ops per second
  let perf (count:int64) (message:string) (f:unit -> unit) : unit = // int * int =
    GC.Collect(3, GCCollectionMode.Forced, true)
    let startMem = GC.GetTotalMemory(false)
    let sw = Stopwatch.StartNew()
    f()
    sw.Stop()
    let endtMem = GC.GetTotalMemory(true)
    let p = (1000L * count/sw.ElapsedMilliseconds)
    //int p, int((endtMem - startMem)/1024L)
    Console.WriteLine(message + ", #{0}, ops: {1}, mem/item: {2}", 
      count.ToString(), p.ToString(), ((endtMem - startMem)/count).ToString())


  let IntMap64(count:int64) =
    let intmap = ref IntMap64Tree<int64>.Nil
    perf count "IntMap64<int64> insert" (fun _ ->
      for i in 0L..count do
        intmap := IntMap64Tree.insert ((i)) i !intmap
    )
    perf count "IntMap64<int64> read" (fun _ ->
      for i in 0L..count do
        let res = IntMap64Tree.find i !intmap
        if res <> i then failwith "IntMap64 failed"
        ()
    )
    Console.WriteLine("----------------")
  [<Test>]
  let IntMap64_run() = IntMap64(1000000L)


  let DeedleSeries(count:int64) =
    let deedleSeries = ref (Series.ofObservations([]))
    perf count "DeedleSeries insert" (fun _ ->
      for i in 0L..count do
        deedleSeries := Series.merge !deedleSeries (Series.ofObservations([i => i]))
    )
    perf count "DeedleSeries read" (fun _ ->
      for i in 0L..count do
        let res = Series.lookup i Deedle.Lookup.Exact !deedleSeries 
        if res <> i then failwith "DeedleSeries failed"
        ()
    )
    Console.WriteLine("----------------")
  [<Test>]
  let DeedleSeries_run() = DeedleSeries(10000L)


  let FSXVector(count:int64) =
    let vec = ref FSharpx.Collections.PersistentVector.empty
    perf count "FSXVector insert" (fun _ ->
      for i in 0L..count do
        vec := vec.Value.Update(int32(i), i)
    )
    perf count "FSXVector read" (fun _ ->
      for i in 0L..count do
        let res = vec.Value.Item(int32(i))
        if res <> i then failwith "FSXVector failed"
        ()
    )
    Console.WriteLine("----------------")
  [<Test>]
  let FSXVector_run() = FSXVector(1000000L)



  let FSXDeque(count:int64) =
    let vec = ref FSharpx.Collections.Deque.empty
    perf count "FSXDeque Conj" (fun _ ->
      for i in 0L..count do
        vec := vec.Value.Conj i
    )
    perf count "FSXDeque Uncons" (fun _ ->
      for i in 0L..count do
        let res, tail = vec.Value.Uncons
        vec := tail
        if res <> i then failwith "FSXVector failed"
        ()
    )
    vec :=FSharpx.Collections.Deque.empty
    perf count "FSXDeque Cons" (fun _ ->
      for i in 0L..count do
        vec := vec.Value.Cons i
    )
    perf count "FSXDeque Unconj" (fun _ ->
      for i in 0L..count do
        let init, res = vec.Value.Unconj
        vec := init
        if res <> i then failwith "FSXVector failed"
        ()
    )
    vec := FSharpx.Collections.Deque.empty
    Console.WriteLine("----------------")
  [<Test>]
  let FSXDeque_run() = FSXDeque(1000000L)

  let FSXHashMap(count:int64) =
    let vec = ref FSharpx.Collections.PersistentHashMap.empty
    perf count "FSXHashMap<int64,int64> insert" (fun _ ->
      for i in 0L..count do
        vec := vec.Value.Add(i, i)
    )
    perf count "FSXHashMap<int64,int64> read" (fun _ ->
      for i in 0L..count do
        let res = vec.Value.Item(i)
        if res <> i then failwith "FSXVector failed"
        ()
    )
    Console.WriteLine("----------------")
  [<Test>]
  let FSXHashMap_run() = FSXHashMap(1000000L)

  let SCGList(count:int64) =
    let list = ref (System.Collections.Generic.List<int64>())
    perf count "SCGList insert" (fun _ ->
      for i in 0L..count do
        list.Value.Insert(int i, i)
      list.Value.TrimExcess()
    )
    perf count "SCGList read" (fun _ ->
      for i in 0L..count do
        let res = list.Value.Item(int32(i))
        if res <> i then failwith "SCGList failed"
        ()
    )
    list := (System.Collections.Generic.List<int64>())
    perf 10000L "SCGList insert first" (fun _ ->
      for i in 0L..10000L do
        list.Value.Insert(0, i)
      list.Value.TrimExcess()
    )
    list := (System.Collections.Generic.List<int64>())
    perf 100000L "SCGList insert first" (fun _ ->
      for i in 0L..100000L do
        list.Value.Insert(0, i)
      list.Value.TrimExcess()
    )
    Console.WriteLine("The bigger the list, the more data is copied")
    list := null
    Console.WriteLine("----------------")
  
  [<Test>]
  let SCGList_run() = SCGList(1000000L)


  let DeedleDeque(count:int64) =
    let deque = ref (DeedleDeque.Deque())
    perf count "DeedleDeque Add" (fun _ ->
      for i in 0L..count do
        deque.Value.Add i
    )
    perf count "DeedleDeque RemoveFirst" (fun _ ->
      for i in 0L..count do
        let res = deque.Value.RemoveFirst()
        if res <> i then failwith "DeedleDeque failed"
        ()
    )
    deque := DeedleDeque.Deque()
    Console.WriteLine("----------------")
  [<Test>]
  let DeedleDeque_run() = DeedleDeque(1000000L)


  let MapTree(count:int64) =
    let map = ref MapTree.empty
    perf count "MapTree Add" (fun _ ->
      for i in 0L..count do
        map := MapTree.add MapTree.fgc (i) i !map
    )
    perf count "MapTree Read" (fun _ ->
      for i in 0L..count do
        let res = MapTree.find MapTree.fgc (i) !map
        if res <> i then failwith "MapTree failed"
        ()
    )
    map := MapTree.empty
    perf count "MapTree Add Reverse" (fun _ ->
      for i in 0L..count do
        map := MapTree.add MapTree.fgc (count - i) i !map
    )
    perf count "MapTree Read Reverse" (fun _ ->
      for i in 0L..count do
        let res = MapTree.find MapTree.fgc (i) !map
        if res <> count - i then failwith "MapTree failed"
        ()
    )
    Console.WriteLine("----------------")
  [<Test>]
  let MapTree_run() = MapTree(1000000L)


  let SCGSortedList(count:int64) =
    let sl = ref (SortedList())
    perf count "SCGSortedList Add" (fun _ ->
      for i in 0L..count do
        sl.Value.Add(i, i)
    )
    perf count "SCGSortedList Read" (fun _ ->
      for i in 0L..count do
        let res = sl.Value.Item(i)
        if res <> i then failwith "SCGSortedList failed"
        ()
    )
    sl := SortedList()
    let count = count / 10L
    perf count "SCGSortedList Add Reverse" (fun _ ->
      for i in 0L..count do
        sl.Value.Add(count - i, i)
    )
    perf count "SCGSortedList Read Reverse" (fun _ ->
      for i in 0L..count do
        let res = sl.Value.Item(count - i)
        if res <> i then failwith "SCGSortedList failed"
        ()
    )
    Console.WriteLine("----------------")
  [<Test>]
  let SCGSortedList_run() = SCGSortedList(1000000L)

  let SortedMapTest(count:int64) =
    let smap = ref (SortedMap())
    perf count "SortedMap Add" (fun _ ->
      for i in 0L..count do
        smap.Value.Add(i, i)
    )
    perf count "SortedMap Read" (fun _ ->
      for i in 0L..count do
        let res = smap.Value.Item(i)
        if res <> i then failwith "SortedMap failed"
        ()
    )
    smap := SortedMap()
    let count = count / 10L
    perf count "SortedMap Add Reverse" (fun _ ->
      for i in 0L..count do
        smap.Value.Add(count - i, i)
    )
    perf count "SortedMap Read Reverse" (fun _ ->
      for i in 0L..count do
        let res = smap.Value.Item(count - i)
        if res <> i then failwith "SortedMap failed"
        ()
    )
    Console.WriteLine("----------------")
  [<Test>]
  let SortedMap_run() = SortedMapTest(1000000L)


  let MapDequeTest(count:int64) =
    let mdq = ref (MapDeque())
    perf count "MapDeque Add" (fun _ ->
      for i in 0L..count do
        mdq.Value.Add(i, i)
    )
    perf count "MapDeque Read" (fun _ ->
      for i in 0L..count do
        let res = mdq.Value.Item(i)
        if res <> i then failwith "MapDeque failed"
        ()
    )
    mdq := MapDeque()
    let count = count / 10L
    perf count "MapDeque Add Reverse" (fun _ ->
      for i in 0L..count do
        mdq.Value.Add(count - i, i)
    )
    perf count "MapDeque Read Reverse" (fun _ ->
      for i in 0L..count do
        let res = mdq.Value.Item(count - i)
        if res <> i then failwith "MapDeque failed"
        ()
    )
    Console.WriteLine("----------------")
  [<Test>]
  let MapDeque_run() = MapDequeTest(1000000L)

  let SHM(count:int64) =
    let shm = ref (SortedHashMap(Int64HashComparer(256us)))
    perf count "SHM<1024> Add" (fun _ ->
      for i in 0L..count do
        shm.Value.Add(i, i)
    )
    perf count "SHM Read" (fun _ ->
      for i in 0L..count do
        let res = shm.Value.Item(i)
        if res <> i then failwith "SHM failed"
        ()
    )
    shm := (SortedHashMap(Int64HashComparer(256us)))
    let count = count / 10L
    perf count "SHM<1024> Add Reverse" (fun _ ->
      for i in 0L..count do
        shm.Value.Add(count - i, i)
    )
    perf count "SHM Read Reverse" (fun _ ->
      for i in 0L..count do
        let res = shm.Value.Item(count - i)
        if res <> i then failwith "SHM failed"
        ()
    )
    Console.WriteLine("----------------")
  [<Test>]
  let SHM_run() = SHM(1000000L)

  [<Test>]
  let ``Run all``() =
    Console.WriteLine("VECTORS")
    FSXVector_run()
    SCGList_run()

    Console.WriteLine("DEQUE")
    FSXDeque_run()
    DeedleDeque_run()

    Console.WriteLine("MAPS/SERIES")
    DeedleSeries_run()
    FSXHashMap_run()
    IntMap64_run()
    MapTree_run()
    SCGSortedList_run()
    SortedMap_run()
    //MapDeque_run() // bugs!
    SHM_run()
    Console.WriteLine("Profile SHM! Performance must be above 5M/sec, see the test ``Nested sorted list - optimized`` in SortedMapTests")