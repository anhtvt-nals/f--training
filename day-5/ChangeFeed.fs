module ChangeFeed

open Microsoft.Azure.Cosmos
open Types
open Category
open Book
open System.Threading.Tasks
open System.Collections.Generic

let createBookChangeFeedProcessor 
    (bookContainer: Container) 
    (leaseContainer: Container) =
    let handler = 
        Container.ChangesHandler<Book>(fun changes cancellationToken ->
            handleBookChanges bookContainer changes
        )
    bookContainer
        .GetChangeFeedProcessorBuilder<Book>("book-change-feed-processor", handler)
        .WithInstanceName("book-change-feed-instance")
        .WithLeaseContainer(leaseContainer)
        .Build()

   