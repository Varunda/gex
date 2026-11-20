

export default class Base64Util {

    // https://stackoverflow.com/a/15016605
    // why a golfed one? idk lmao
    public static decode(b64: string): string {
        var e: any={},i,b=0,c,x,l=0,a,r='',w=String.fromCharCode,L=b64.length;
        var A="ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
        for(i=0;i<64;i++){e[A.charAt(i)]=i;}
        for(x=0;x<L;x++){
            c=e[b64.charAt(x)];b=(b<<6)+c;l+=6;
            while(l>=8){((a=(b>>>(l-=8))&0xff)||(x<(L-2)))&&(r+=w(a));}
        }
        return r;
    }

}