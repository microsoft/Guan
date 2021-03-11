# Guan

Guan is a general-purpose logic programming system composed of a C# API and logic engine. It enables Prolog style syntax for writing logic rules and the inter-op between such rules with regular C# code. External predicates are written in C# (the API piece) and logic rules (structured text) can be housed in simple text files or as string variables in your consuming program. These logic rules will be parsed and executed by Guan, which provides imperative, procedural, and even functional programming idioms the expressive power of logic programming for use in several novel contexts, not the least of which is configuration-as-logic (see [FabricHealer](https://github.com/microsoft/service-fabric-healer) for a concrete example of Configuration-as-Logic using Guan.).

Author: Lu Xun, Microsoft.

### Syntax

As stated above, Guan uses [Prolog style syntax](http://www.learnprolognow.org/index.php). We will not describe things that are common with standard Prolog, but rather present the differences below (they are different mostly to allow more natural interop between rules and C# code): 

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

```
"||", "&&", "==", "!=", ">", ">=","<","<=","+","-","*","/" 
```
 
Evaluated compound terms can be nested. For example: 

```Prolog
?t2 > add(?t1, -00:10:00) && ?v1 == ?v2 
```

This is how logical programming in Guan handles arithmetic operations and comparisons, which is quite different than standard Prolog. 
Since we are free to add new functions which invoke arbitrary C# logic, Guan can provide capabilities that are not possible in Prolog.

When a goal contains a function, it becomes a constraint and the goal is considered satisfied if and only if the evaluation is “True” 
(typically such goal should return Boolean result, but if the result is not Boolean, we treat null and empty string as False and everything else as True). 
If the function can’t be evaluated because of un-instantiated variables, the constraint will be passed along for the remaining goals, until the variables are instantiated. 
If there are still variables un-instantiated when there is no more goal left in the rule, the constraint will be ignored. 



### Built-in Predicates (aka System Predicates)

 
Guan provides some standard built-in predicates. Many of them have already been described in the examples, for the others please refer to documentation for standard Prolog 
```
assert 
! (cut) 
fail 
not 
var 
nonvar 
atom 
compound 
ground 
= (unify) 

Some commonly used predicates for list related operations: 

append 
member 
length 
reverse 
```

Popular Prolog implementations typically have many more defined, which might be added to Guan in the future. 

The below predicates are either unique to Guan, or have some special semantics: 


**enumerable**: takes a C# collection object as the first argument and returns the members as the second argument. 

**forwardcut**: 

For temporal reasoning, an event often needs to be matched with another event that is closest to it. For example, consider a sequence of events of Start, End, Start, End, Start, Start, End… Such sequence could be the start and end of a process as an example. Note that sometimes the End event is missing, as the process could have crashed. Now suppose that we want to define a frame type ProcessStartEnd. We cannot use a simple rule like this: 

```Prolog
ProcessStartEnd(?StartTime, ?EndTime) :- Start(time=?StartTime),
    End(time=?EndTime),
    ?EndTime > ?StartTime 
```

As this could match the start of a process instance with the end of a later instance. Of course, if there is a unique process id we can use, such problem can be avoided. But what if there is no such id and all we can rely on is the order of the events? One solution is to use negation as failure: 

```Prolog
ProcessStartEnd(?StartTime, ?EndTime) :- Start(time=?StartTime), 
    End(time=?EndTime), 
    ?EndTime > ?StartTime, 
    not Between(?StartTime, ?EndTime) 

Between(?StartTime, ?EndTime) :- Start(time=?t), 
    ?t >?StartTime, 
    ?t < EndTime 
```

This works, but it requires the trace to be searched twice (assuming the implementation of the Start and End depends on some linear scan of trace). 

Guan provides an alternative: when the Start event is found, in addition to search for the End event, we search for the next Start event in parallel. This is as if we are expanding the search tree at different levels simultaneously. Whenever the search for the Start is found, the pending search for End, if any, is cancelled. Below is the rule for this approach: 

```Prolog
ProcessStartEnd(?StartTime, ?EndTime) :- Start(time=?StartTime),
    forwardcut,
    End(time=?EndTime), 
    ?EndTime > ?StartTime 
```

It is almost the same as the initial wrong version, except that a “forwardcut” goal is added, instructing the infrastructure to keep expanding the choice points for the goal before it while exploring the next goals and perform the cancellation as appropriate. Note that this is again relying on the multiplexing behavior of the parallel tasks. 

In general, the sequential backtracking is a restriction of the original Prolog search mechanism and there are various tweaks we can add to allow the user to change the search policy (parallel search is a common variant) and there might be some special options that are useful for temporal (or spatial) reasoning tasks, which can be an interesting research topic for the future.  

**getval**: retrieve the value of a global variable and unify it with a local logical variable. 

**setval**: set the value of a global variable. If the rule is backtracked, then the global variable value will also be reset to its previous value (SWI-Prolog calls it b_setval). 

**nb_setval**: same as setval, except that the value does not get reset during backtracking. 

**is**: in standard Prolog, it is used for arithmetic operations. In Guan, since compound term can be evaluated automatically, this is typically not needed. When used, it just performs the evaluation of the second argument and unify the result with the first argument. For that purpose, the unify (“=”) predicate can also be used. 

**trace**: enable tracing of rule execution. 

**notrace**: disable tracing. 

**WriteLine**: output to console and trace  
  

### External Predicate 

In addition to the predicates defined by rules and the built-in predicates, sometimes we need to implement new predicates, typically for interacting with external environment. In such case, a subclass of PredicateType needs to be created with the following member implemented: 

public override PredicateResolver CreateResolver(CompoundTerm input, Constraint constraint, QueryContext context) 

 

The implementation should usually create a subclass of PredicateResolver and returns an instance of this subclass in this above method. 

Below we will describe two simple types of external predicates, one for output and one for input. 

To implement a predicate type for output (e.g. a repair action), typically we will want to derive its PredicateResolver from a simple base class: BooleanPredicateResolver. Such a predicate defines a abstract method that is to be implemented: 

protected abstract bool Check()  

If this function returns true, the corresponding goal will succeed and otherwise fail. The function can perform arbitrary logic, based on the input arguments passed in. 
``` C#
protected override bool Check() 
{ 
    string format = Input.Arguments[0].Value.GetEffectiveTerm().GetStringValue(); 
    object[] args = new object[Input.Arguments.Count - 1]; 
    for (int i = 1; i < Input.Arguments.Count; i++) 

    { 
        args[i - 1] = Input.Arguments[i].Value.GetEffectiveTerm(); 
    } 

    EventLog.WriteInfo("WriteLine", format, args); 
    return true; 
} 

 ```

The above is the implementation for the built-in predicate “WriteLine”. We can see that it gets the input values from the Input.Arguments collection. Note that to get the argument value, GetEffectiveTerm() usually needs to be called, which will deal with arguments that contain variables that have been instantiated. For simple predicates used for output, the input should be grounded, i.e. all variables should have been instantiated with a value. The argument returned from GetEffectiveTerm () is a Term, if it is constant, GetValue() can be called to get the C# object value or GetStringValue() can be called if the constant is a string. 

Input predicates usually take in some arguments to specify the query parameter, the rest arguments will contain the query result. We will just consider the case where the query result contains no variables which should be the most typical case when implementing a predicate to query regular data source. 

In such case, the base class GroundPredicateResolver should be used, which has the following abstract method to be implemented: 

```C#
protected abstract Task<Term> GetNextTermAsync(); 
```

Each result should be represented as a Term (typically a CompoundTerm) and returned in this method. When there are multiple results, the method will be invoked multiple times, unless the rule only wants to call the corresponding goal once (e.g. when cut is being used). 

To retrieve the query criteria, the value of input arguments can be examined, same as what is described for the output predicates. 

 

### Debugging 

A convenient to debug rules is by examining the trace. Guan can output trace when the rules are executed. For example, below are the trace for executing the query “append(?X, ?Y, [1,2,3])”: 

```
2020-2-26 10:39:33.283 
Trace 
1 
Call	[1,0]: append(?$0,?$1,[1,2,3]) 

2020-2-26 10:39:33.284 
Trace 
1 
Exit	[1,1]: append([],[1,2,3],[1,2,3]) 

2020-2-26 10:39:33.285 
Trace 
1 
Call	[1,1]: append(?$0,?$1,[1,2,3]) 
 
2020-2-26 10:39:33.286 
Trace 
1 
Call	[2,0]: append(?Xs,?$1,[2,3]) 
 
2020-2-26 10:39:33.286 
Trace 
1 
Exit	[2,1]: append([],[2,3],[2,3]) 

2020-2-26 10:39:33.286 
Trace 
1 
Exit	[1,2]: append([1],[2,3],[1,2,3]) 

2020-2-26 10:39:33.287 
Trace 
1 
Call	[1,2]: append(?$0,?$1,[1,2,3]) 

2020-2-26 10:39:33.287 
Trace 
1 
Call	[2,1]: append(?Xs,?$1,[2,3]) 

2020-2-26 10:39:33.287 
Trace 
1 
Call	[3,0]: append(?Xs,?$1,[3]) 

2020-2-26 10:39:33.287 
Trace 
1 
Exit	[3,1]: append([],[3],[3]) 

2020-2-26 10:39:33.287 
Trace 
1 
Exit	[2,2]: append([2],[3],[2,3]) 

2020-2-26 10:39:33.287 
Trace 
1 
Exit	[1,3]: append([1,2],[3],[1,2,3]) 

2020-2-26 10:39:33.288 
Trace 
1 
Call	[1,3]: append(?$0,?$1,[1,2,3]) 

2020-2-26 10:39:33.288 
Trace 
1 
Call	[2,2]: append(?Xs,?$1,[2,3]) 

2020-2-26 10:39:33.288 
Trace 
1 
Call	[3,1]: append(?Xs,?$1,[3]) 

2020-2-26 10:39:33.288 
Trace 
1 
Call	[4,0]: append(?Xs,?$1,[]) 

2020-2-26 10:39:33.288 
Trace 
1 
Exit	[4,1]: append([],[],[]) 

2020-2-26 10:39:33.288 
Trace 
1 
Exit	[3,2]: append([3],[],[3]) 

2020-2-26 10:39:33.289 
Trace 
1 
Exit	[2,3]: append([2,3],[],[2,3]) 

2020-2-26 10:39:33.289 
Trace 
1 
Exit	[1,4]: append([1,2,3],[],[1,2,3]) 

2020-2-26 10:39:33.289 
Trace 
1 
Call	[1,4]: append(?$0,?$1,[1,2,3]) 

2020-2-26 10:39:33.289 
Trace 
1 
Call	[2,3]: append(?Xs,?$1,[2,3]) 


2020-2-26 10:39:33.289 
Trace 
1 
Call	[3,2]: append(?Xs,?$1,[3]) 

2020-2-26 10:39:33.290 
Trace 
1 
Call	[4,1]: append(?Xs,?$1,[]) 

2020-2-26 10:39:33.290 
Trace 
1 
Fail	[4,1]: append(?Xs,?$1,[]) 

2020-2-26 10:39:33.290 
Trace 
1 
Fail	[3,2]: append(?Xs,?$1,[3]) 

2020-2-26 10:39:33.290 
Trace 
1 
Fail	[2,3]: append(?Xs,?$1,[2,3]) 

2020-2-26 10:39:33.290 
Trace 
1 
Fail	[1,4]: append(?$0,?$1,[1,2,3]) 

 ```

 

There are basically two types of trace, one for calling a goal and another for the result: 

Call: this is when a goal is called. Sometimes you will see T-Call, this is similar to Call, but with tail-optimization. In this trace, you typically see variables in the arguments. 

Exit/Fail: when the goal is successful, you will see the Exit trace, otherwise you will see Fail. Compared with the Call trace, typically you will see some variables getting instantiated. 

Every trace has a pair of numbers like [4,0]. The first number indicates the depth of the call frame. The number will increase for the “Call” trace and decrease for “Exit” or “Fail” trace. You can also match the Call trace with the Exit/Fail trace by this number to see the invocation and result for the same call. The second number is the iteration count. When a goal is initially called, the number is 0, when it returned the number gets incremented by 1. 

Such trace is not enabled by default. And there are two ways to enable them: 

In the application config file, set EnableTrace to True. This will enable trace for the entire process. 

```<add key="EnableTrace" value="True"/> ```

Include a “trace” goal in a rule, this will only enable trace after the goal is executed. Conversely, you can disable trace with a “notrace” goal.  

```Prolog
trace,append(?X, ?Y, [1,2,3]) 
```

The best way to understand the “trace” and “notrace” goal is to think of them as a special setval predicate where the global variable name and value do not need to be specified explicitly. On backtracking, the previous flag for enable/disable trace will be restored. 

When trace is enabled this way, tail optimization is disabled. 

The trace will be output to both console and the .trace file. 

Besides the standard trace, you can add your own by using the WriteLine predicate. For example: 

```Prolog
WriteLine(‘X={0},Y={1}”, ?X, ?Y)
``` 


### Implementing Functions 

As described previously, functions invoking arbitrary C# logic can be used in the rules either as constraints or to perform some operations. They can also be used in records.txt file to parse data into C# objects. In fact, the Guan infrastructure also uses functions for many other purposes which will not be described in this document. 

This section will discuss how the developer can implement his own function. 

The base class for a function that can be used in Guan is GuanFunc, which defines the following abstract method that needs to be implemented: 

```C# 
public abstract object Invoke(IPropertyContext context, object[] args); 
```

The difference between a GuanFunc and a normal function is that GuanFunc can take a context object as input, which allows such function to be context-dependent behavior. During the execution of rules, the context is an object that contains all the global variables, which means that a function can access the global variables if needed. 

Most functions should in rules or records.txt do not depend on the context though. Such function can instead derive from a sub class of GuanFunc called StandaloneFunc, with the abstract method below: 

```C#
public abstract object Invoke(object[] args); 
```

You can see that the only difference is that the context argument is gone. 

If your function takes only one or two input argument(s), you can also derive from UnaryFunc or BinaryFunc. 

After a function class is implemented, it needs to get exposed to the Guan infrastructure so that Guan knows about its existence. The simplest way is to define a singleton instance of the function as a public static property. 

Below is a simple example of a function ToLower: 
```C#
internal class ToLowerFunc : UnaryFunc 
{ 
    public static ToLowerFunc Singleton = new ToLowerFunc(); 

    protected ToLowerFunc() : base("ToLower") 
    { 

    } 

    public override object UnaryInvoke(object arg) 
    { 
        if (arg == null) 
        { 
            return string.Empty; 
        } 
 
        return arg.ToString().ToLower(); 
    } 
} 

```
 


### Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
