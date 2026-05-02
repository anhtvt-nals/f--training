module Number

let isEven x = x % 2 = 0
let isOdd x = x % 2 <> 0

let classify x =
    if x < 0 then "Negative"
    elif x = 0 then "Zero"
    else "Positive"