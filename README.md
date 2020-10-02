# Clean Code Developer Roslyn Analyzers
This project started as an article for the german dotnetpro[https://dotnetpro.de] magazine. It contains Roslyn Analyzers for Clean Code Developer[https://clean-code-developer.com] principles.

The current version analyzes if methods violate the *IOSP*: Integration Operation Segregation Principle. The principle states that a method is either an integration method or an operation. Integration methods call other methods that are defined and implemented in the solution. If one of your methods calls another of your methods its responsibility is to integrate. On the other hand an operation must not call one of your own methods. An operation calls APIs and contains expressions like "x + 2 < 42". API calls are calls to foreign methods that are not defined in your code. For example Console.WriteLine or object.ToString are API calls.

The reasoning behind the IOSP is the following: if we strictly separate integration from operation this leads to a better understanding of the code structure. This is because the abstraction level is clearly separated into high level vs. low level code. Integration methods are on a high level. They only call other methods in order to integrate them. Thus it is easy to understand such a high level integration method. On the other hand an operation is easy to read because it contains code only on a low level. Given that an operation must also conform to the *SRP* (Single Responsibility Principle) it is easy to read and understand an operation because it contains only low level code that solves a specific problem. In order to understand an operation it is not necessary to understand other methods from the solution. It is only necessary to understand calls to runtime or framework functionality.

The second advantage of conforming to the IOSP is testability. If we strictly separate integration from operation we can test the operations in isolation. Because operations don't call other methods from our solution every operation is a separate functional unit. Thus we don't need any mocks in order to separate an operation from its dependencies. It has no dependancies otherwise it would mix operational code with integration. The integeration methods don't contain any logic. So it is not necessary to separate the integration methods from their dependencies. The only job of the integration methods is to integrate calls to other methods. So we test the integration methods with integration tests.

Integration methods can contain control structures like foreach, for, while, if or case statements. They must not contain any expressions. It is ok to call a function that contains the expression:

```
if(x + 2 == 42) { ... }
```

vs.

```
if(ThisIsTheAnswer(x)) { ... }
```
