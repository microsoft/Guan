# Guan

Guan is a general-purpose logic programming system composed of a C# API and logic interpreter/query executor. It enables Prolog style syntax for writing logic rules and executing queries over these rules. External predicates are written in C# (the API piece) and logic rules (structured text) can be housed in simple text files or as string variables in your consuming program. These logic rules will be parsed and executed by Guan, which provides imperative, procedural, and even functional programming idioms the expressive power of logic programming for use in several novel contexts, not the least of which is configuration-as-logic (link to FH). 

Author: Lu Xun, Microsoft.

### Syntax

As stated above, Guan uses [Prolog style syntax](http://www.learnprolognow.org/index.php). We will not describe things that are common with standard Prolog, but rather present the differences below: 

* The trailing ‘.’ Is not needed as delimiter for rules, since in Guan every rule is represented in a separate string. 

* A variable must start with a ‘?’, followed by alphanumeric characters (\w) or underscore. For example: ?a, ?A, ?_a, ?aa_ are all valid variables. Note that this is very different from Prolog where the first character must be capitalized. 

* Predicate name can be any combination of alphanumeric characters and underscore. The predicate name uniquely identifies the predicate type (in standard Prolog, predicate name plus the number of argument uniquely identifies a predicate type). 

* The usage of “;” for the disjunction of goals is currently not supported. Use a separate predicate type with multiple rules for disjunction. 

* Use not(goal) for negation, “\+ goal” is not supported. 

* The arguments of a compound term are named (alphanumeric characters and underscore). For example: TraceRecord(Source=?NodeId, GroupType=P2P.Send, text=?text, time=?t). The argument name can be omitted for the following cases: 

* It is allowed to have positional arguments (like standard Prolog) before the appearance of any named argument. For example, goal(?NodeId, P2P.Send, text=?text, time=?t) is a valid compound term where the first two arguments do not have explicit names. Internally, names are assigned implicitly. The first argument will have a name “0”, and the second with “1”, etc. Note that the argument name “time” for the last argument can’t be omitted, since there is already a named argument “text” before it. 

* If an argument is not positional (appears after some named argument), its name can be inferred from the argument value if one of the following is true: 

* If the argument is a variable, the argument name will be the same as the variable name. For example, TraceRecord(Source=?NodeId, GroupType=P2P.Send, ?text, time=?t). The name of the 3rd argument is inferred to be “text”. 

* If the argument is a compound, the argument name will be the same as the functor name. For example, somegoal(arg0=0, point(1, 2)). The name of the second argument is “point”. 

* The custom behavior of a predicate type might implement type-specific syntax sugar: if the head of a rule with the corresponding type does not have an argument mentioned, the argument will be added automatically with the value being a variable with the same name. For example, suppose “mygoal” is a type which has two arguments v1 and v2, the following rules are all equivalent: 

```Prolog
mygoal :- body 

mygoal(v1=?v1, v2=?v2) :- body 

mygoal(v1=?v1) :- body 
```

### Function & Constraint 

The functor of some compound terms can be evaluated at runtime, when all of its arguments are grounded. They can be considered as functions. For example: 

```Prolog
add(?t1, -00:10:00) 
```

This is a compound term with function “add”. Since “add” is built-in evaluated function, when the variable “?t1” is instantiated, the entire compound term can be evaluated to become a constant term (in this case “?t1” should be a C# DateTime object, which is added with a TimeSpan of minus 10 minutes, so we effectively are getting a timestamp that is 10 minutes earlier than “?t1”). 

```Prolog
eq(?v1, ?v2) 
```

This is another example. The “eq” function will check whether the two arguments are equal (using C# object.Equals), the result is a constant of C# Boolean value. 

Operators are defined for some commonly used functors. Below is a list with the obvious semantics: 

"||", "&&", "==", "!=", ">", ">=","<","<=","+","-","*","/" 

 
Evaluated compound terms can be nested. For example: 

```Prolog
?t2 > add(?t1, -00:10:00) && ?v1 == ?v2 
```

This is how logical programming in Guan handles arithmetic operations and comparisons, which is quite different from standard Prolog. Since we are free to add new functions which invokes arbitrary C# logic, Guan can provide functionalities that not possible in Prolog. The handling of timestamp is a simple example. 

When a goal contains a function, it becomes a constraint and the goal is considered satisfied if and only if the evaluation is “True” (typically such goal should return Boolean result, but if the result is not Boolean, we treat null and empty string as False and everything else as True). If the function can’t be evaluated because of un-instantiated variables, the constraint will be passed along for the remaining goals, until the variables are instantiated. If there are still variables un-instantiated when there is no more goal left in the rule, the constraint will be ignored. 


### Global Variables 

 
Other than the logical variables that can be used in rules, many Prolog implementations provide something called “global variable”, which can be used across rules. One place where global variable can be convenient is when we want to pass some value to a rule several levels deep. If normal logical variable is used, they will have to be passed along each intermediate rule. In Guan, global variables are mostly used for passing the time range for searching the symptom. Many rules will need to use the time range so using global variable to make the range accessible from all the rules is quite convenient. 

There are three built-in predicates related to global variable (similar to what SWI-Prolog provides): 

* getval: retrieve the value of a global variable and unify it with a local logical variable. 

* setval: set the value of a global variable, when the rule is backtracked, the global variable value will also be reset to its previous value (SWI-Prolog calls it b_setval). 

* nb_setval: same as setval, except that the value is not getting reset during backtracking. 

As an example, the below goal will retrieve the begin time of the search range and unify it with the logical variable “?StartTime” 

```Prolog
getval(RangeBegin, ?StartTime) 
```
And the one below sets the begin time to be one minute later than “?t”. 

```Prolog
setval(RangeBegin, add(?t, 00:01:00)) 
```

# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
