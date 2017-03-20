**This task allows you to associate Java method tests to Test Plan work items.**

In a Test Plan the automated tests association is possible only for .NET projects.
This extension creates a *dynamic .NET library* that can be used to link the Java test result with a Test Plan work items.

The dynamic .NET library will contains .NET Unit tests that read the results of the last Java test execution. The .NET Unit tests are dynamically associated to the Test Plan.

This task must be executed after a Maven build and expects the test results to be in the form **TEST-ClassName.MethodName.xml**. You need to provide a JSON that must map each java test with a test plan workitem (ID):

```json
[
  {
    "className": "HelloWorldJava.Demo.HelloWorldJunitTest",
    "methodName": "testTrue",
    "workItemID": 216
  },
  {
    "className": "HelloWorldJava.Demo.HelloWorldJunitTest",
    "methodName": "testFalse",
    "workItemID": 217
  }
]
```

You can create a JSON file and add to the source directory or you can provide the JSON string as input (escaped) argument:

```
[{\"className\": \"HelloWorldJava.Demo.HelloWorldJunitTest\",\"methodName\": \"testTrue\",\"workItemID\": 216}]
```

After this task, you genereally want to execute a Functional Test task providing the Test Plan to run. The results will be reported and integrated in the Test Plan view.

With such extension you will be able to execute the automated tests of a Test plan even for a pure Java project as part of your build process.