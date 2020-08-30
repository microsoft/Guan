# Guan

Guan is a general-purpose C# logic programming API and interpreter that employs Prolog style syntax for writing logic rules.

Author: Lu Xun, Microsoft.

### Summary

As stated above, Guan uses Prolog style syntax. We will not describe things that are common with standard Prolog, but rather present the differences below: 

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

* The custom behavior of a predicate type might implement type-specific syntax sugar. This is the case for all symptom types defined in the model. As described in the previous section, when a symptom type is defined, meta-data about is arguments are provided. The symptom type can thus use the information to automatically add arguments to a symptom predicate. More specifically, the syntax sugar for symptom type is that: if the head of a rule with the corresponding symptom type does not have an argument mentioned, the argument will be added automatically with the value being a variable with the same name. Furthermore, positional arguments are not allowed for symptom types. For example, suppose “mygoal” is a symptom type which has two arguments v1 and v2, the following rules are all equivalent: 

```Prolog
mygoal :- body 

mygoal(v1=?v1, v2=?v2) :- body 

mygoal(v1=?v1) :- body 
```

* There is a special case for the “StartTime” and “EndTime” arguments for symptom types. They are present in every symptom type without the need for explicit definition. If a symptom appears in the head of a rule and these two arguments are not specified, they will be added automatically using the syntax sugar just mentioned. However, there is an exception: if the rule has a variable “?time” in some of its goals, then both the StartTime and EndTime arguments are assumed to use this “?time” variable as the argument value. This is for the case where a symptom has only a single timestamp instead of a time range. 

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
