int cnt,pwm[16]; //计数器，保存pwm数值
const int led[]={3,5,6,9,10}; //led引脚号
uint8_t mi[3],cmd,cnl,rt[3]; //midi协议数据，命令、通道号，发送给PC的数据
const uint8_t invld[]={0xfa,0x7f,0x7f}; //用于非法数据返回码
uint32_t pre,period=1000; //维护前一次的时间戳，发送给PC的周期（ms）
void show(uint8_t data[3]) { Serial.write(data,3); } //发送PC数据
int ntc() { return analogRead(A0); } //返回温度ADC
int rcds() { return analogRead(A1); } //返回光强ADC
int getVal() { return ((int)mi[2]<<7)|mi[1]; } //解析midi数据的实际值
void setrt(uint8_t head, uint16_t x) //设置返回码
{
  rt[0]=head;
  rt[1]=(x&0x7f);
  rt[2]=((x>>7)&0x7f);
}

void setup()
{ 
  Serial.begin(115200); //115200波特率
  for (int i=0; i<5; i++) {
    int r=led[i];
    pinMode(r,OUTPUT); //设定输出引脚
    digitalWrite(r,HIGH);
    pwm[r]=1023; //保存pwm值
  }
}

void loop()
{
  if (millis()-pre>period) { //大于一个周期没有收到数据，返回PC监听值
    pre=millis();
    setrt(0x80,ntc()); show(rt); //返回温度ADC的监听值
    setrt(0x81,rcds()); show(rt); //返回光强ADC的监听值
  }
  
  if(Serial.available()>0)
  {
    pre=millis(); //重置时间戳
    
    uint8_t ch=Serial.read(); //读取PC数据
    if (ch>>7) cnt=0; //midi协议头部
    mi[cnt++]=ch; //保存收到的数据
    
    if (cnt==3) { //达到midi协议的3字节
      cnt=0; //重置计数器

      cmd=(mi[0]>>4)&0xf; //解析命令
      cnl=mi[0]&0xf; //解析通道号

      memcpy(rt,invld,sizeof(rt)); //默认是非法数据
      switch (cmd) {
        case 0x9:
          if (mi[1]<2) {
            digitalWrite(cnl,mi[1]); //设置引脚电平
            setrt(mi[0],digitalRead(cnl));
          }
          break;
        case 0xa: //set period
          period=getVal(); //自定义，设置周期
          break;
        case 0xc:
          if (mi[1]==0x66) setrt(mi[0],digitalRead(cnl)); //读取引脚电平
          break;
        case 0xd:          
          if (!(mi[2]>>6)) {
            int tmp=mi[1]|((mi[2]&1)<<7);
            analogWrite(cnl,tmp); //pwm写
            pwm[cnl]=map(tmp,0,255,0,1023); //维护pwm值
          }
          setrt(mi[0],pwm[cnl]); //返回对应值
          break;
        case 0xe:
          if (cnl<0x8 && mi[1]==0x11 && mi[2]==0x11) {
            setrt(mi[0],analogRead(cnl)); //读取ADC值
          }
          break;
        case 0xf:
          if (mi[1]==0x55 && mi[2]==0x55) {
            if (cnl==0xf) setrt(mi[0],millis()); //返回时间戳
            else if (cnl==0x9) setrt(mi[0],8061); //返回学号
          }
          break;
        default:
          break;
      }
      show(rt); //发送数据
      
    }
  }
}