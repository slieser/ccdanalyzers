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
        }
    }

    private void Operation3(int i) {
    }
}