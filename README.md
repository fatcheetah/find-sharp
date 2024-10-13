# find-sharp

Simple tool to find files/directories in your linux filesystem.

+ multi-threaded || task-driven
+ Knuth-Morris-Pratt algorithm search
+ C# interop

### searching examples

I want my search to be easy and not require a man page

```sh
$ find-sharp alpha1
/home/cream/a/service2/alpha1
/home/cream/a/service/alpha1
```

```sh
$ find-sharp alpha
/home/cream/a/service/Alpha4
/home/cream/a/service/alpha1
/home/cream/a/service/alpha2
/home/cream/a/service2/Alpha4
/home/cream/a/service/Alpha3
/home/cream/a/service2/alpha1
/home/cream/a/service2/alpha2
/home/cream/a/service2/Alpha3
```

```sh
$ find-sharp Alpha
/home/cream/a/service/Alpha4
/home/cream/a/service2/Alpha4
/home/cream/a/service/Alpha3
/home/cream/a/service2/Alpha3
```

```sh
$ find-sharp /service
/home/cream/a/service
/home/cream/a/service2
```

```sh
$ find-sharp service/
/home/cream/a/service/Alpha4
/home/cream/a/service/bravo1
/home/cream/a/service/alpha1
/home/cream/a/service/alpha2
/home/cream/a/service/bravo2
/home/cream/a/service/Alpha3
```

```sh
$ find-sharp service/alpha
/home/cream/a/service/Alpha4
/home/cream/a/service/alpha1
/home/cream/a/service/alpha2
/home/cream/a/service/Alpha3
```

<br>

#### notes

+ always recursive
+ always smartcase
+ small code extended when unable to find something
+ multi-threaded means no sorting, use your pipes `$ find-sharp search | sort `
+ directory is envoke location

<br>

### Build (publish) and Run

```
$ dotnet publish -c release -o pub --self-contained;
$ ./pub/find-sharp
```

<br>

### *benchmark*

*this isn't a science test*

**find-sharp**
```sh
$ time find-sharp kmp
/home/cream/fun/find-sharper/kmp.cs
/home/cream/fun/find-sharp/find-sharp/kmp.cs
/usr/src/linux-headers-6.1.0-23-amd64/include/config/TEXTSEARCH_KMP
/usr/src/linux-headers-6.1.0-25-amd64/include/config/TEXTSEARCH_KMP
/usr/lib/modules/6.1.0-25-amd64/kernel/lib/ts_kmp.ko
/usr/lib/modules/6.1.0-23-amd64/kernel/lib/ts_kmp.ko
/media/g/SteamLibrary/steamapps/common/Dawn of War 2/GameAssets/Locale/English/english_OrkMPSpeech_DELTA.sga
/media/g/SteamLibrary/steamapps/common/Dawn of War 2/GameAssets/Locale/English/english_orkmpspeech.sga

real    0m0.404s
user    0m1.323s
sys     0m1.203s
```
**find**
```sh
$ time find -iname '*kmp*' 2>/dev/null
./home/cream/fun/find-sharper/kmp.cs
./home/cream/fun/find-sharp/find-sharp/kmp.cs
./media/g/SteamLibrary/steamapps/common/Dawn of War 2/GameAssets/Locale/English/english_OrkMPSpeech_DELTA.sga
./media/g/SteamLibrary/steamapps/common/Dawn of War 2/GameAssets/Locale/English/english_orkmpspeech.sga
./usr/lib/modules/6.1.0-23-amd64/kernel/lib/ts_kmp.ko
./usr/lib/modules/6.1.0-25-amd64/kernel/lib/ts_kmp.ko
./usr/src/linux-headers-6.1.0-23-amd64/include/config/TEXTSEARCH_KMP
./usr/src/linux-headers-6.1.0-25-amd64/include/config/TEXTSEARCH_KMP

real    0m1.614s
user    0m0.349s
sys     0m1.247s
```
