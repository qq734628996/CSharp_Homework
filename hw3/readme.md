# Homework3

## Requirements

1. Please build a Desktop WPF APP. for its functions please refer to the viode "ABSWP81Part9_ Tip Calculator_mid.MP4".
2. Deadline of this APP is on April 4.2019. same as homework 1,2 please upload your APP source code on Github with Readme.md

## Result

![](video\sample.gif)

Fix a bug: `System.NullReferenceException`, since `selectedRadio` may be null: 

``` csc
if (selectedRadio != null)
{
    
}
```