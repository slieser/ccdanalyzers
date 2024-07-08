using System;

internal class A
{
    public void Integration1() {
        Operation1();
        Operation2();
        var s = 42.ToString();
    }

    public void Integration2() {
        Operation3(1 + 1);
        Operation3(1 + 1);
    }

    public void Operation1() {
        var x = 5;
        if (x + 1 == 42) {
            Console.WriteLine(x);
            Operation1();
        }
    }

    public void Operation2() {
        Operation1();
        Console.WriteLine(42);
    }

    public void Integration3() {
        for (var i = 0; i < 10; i++) {
            Operation3(i);
            throw new Exception();
        }
    }

    private void Operation3(int i) {
    }

    public void Integration4() {
        try {
            Operation1();
        }
        catch {
            Operation2();            
        }     
    }

    public void Integration5() {
        try {
            Operation1();
        }
        catch (Exception e) {
            Operation4(e.Message);            
        }     
    }

    private void Operation4(string exception) {
    }

    public void Operation() {
        void OperationIntern() {
            var x = 3 + 5;          // Expression => Operation
        } 
        
        Console.WriteLine();        // API Call => Operation
        OperationIntern();          // Local Method Call => Integration
    }

    public void Integration6(Action action) {
        Operation1();
        action();       // should be Integration!
    }

    public void Operation6(Func<int> func) {
        var i = 42 + func();
    }
    
    public delegate void MyDelegate();
    
    public void Integration7(MyDelegate myDelegate) {
        Operation1();
        myDelegate();
    }
    
    public void Operation7() {
        var myDelegate = new MyDelegate(Operation1);
        myDelegate();
        var i = 42 + 1;
    }

    public class HasEvent
    {
        public event Action MyEvent;

        public void Fire() {
            MyEvent?.Invoke();
        }
    }
    
    public void Integration8(HasEvent hasEvent) {
        Operation1();
        hasEvent.MyEvent += Operation1;
        hasEvent.Fire();
    }
}