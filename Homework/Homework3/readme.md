# Homework3

## Requirements

Build a Desktop WPF APP. For its functions refer to the video [ABSWP81Part9_ Tip Calculator_mid.MP4](https://channel9.msdn.com/Series/Windows-Phone-8-1-Development-for-Absolute-Beginners).

## Result

![](video/sample.gif)

Fix a bug: `System.NullReferenceException`, since `selectedRadio` may be null: 

``` c#
if (selectedRadio != null)
{
    
}
```