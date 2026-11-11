const fs=require('fs')
let s=fs.readFileSync('pages/login/login.wxml','utf8')
const open='<view wx:if="{{logo}}" style="position:absolute;left:-999rpx;width:0;height:0;overflow:hidden;">'
const end='</svg></view>'
const a=s.indexOf(open)
const b=s.indexOf(end)
console.log(a,b,s.length)
if(a>=0 && b>=0) s=s.slice(0,a)+s.slice(b+end.length)
fs.writeFileSync('pages/login/login.wxml',s)
