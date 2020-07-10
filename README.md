# daily-quest-checker

Command Line Interface daily quest checker

You can use it for Maplestory, Etc...

## Requirements

.NET Core 3.1

## Build

It is required .NET Core 3.1 SDK.
Please install it from [link](https://dotnet.microsoft.com/download)

```powershell
git clone https://github.com/pid011/daily-quest-checker.git
cd daily-quest-checker
dotnet publish -c Release -o [publish directory]
```

## 사용 방법

프로그램을 처음 실행하면 config와 database폴더가 프로그램이 위치한 폴더에 생성됩니다.

제일 먼저 config폴더에 있는 `daily-quest.default.txt`를 열어서 다음과 같이 수정해주어야 합니다.

### daily-quest.default.txt

`daily-quest.default.txt`의 첫번째 줄에는 프로그램에서 이모지를 사용할 지에 대한 여부를 설정할 수 있습니다. 만약 프로그램을 실행할 터미널이 이모지를 지원한다면 `true`를, 아니라면 `false`를 써주세요.

그 다음줄 부터는 일일퀘스트 항목을 한 줄에 하나씩 적어주세요.
중간에 비어있는 줄이 있어도 프로그램에서는 무시되니 보기 좋게 종류별로 문단을 나눠 작성하는 것도 하나의 방법입니다.

일일퀘스트 항목을 다 작성하면 `daily-quest.default.txt`의 내용은 최종적으로 다음과 같을 것입니다.

```plaintext
true

daily quest 1
daily quest 2
daily quest 3

daily quest A
daily quest B
daily quest C
```

### DailyQuestChecker

터미널에서 `.\DailyQuestChecker.exe`를 실행하여 일일퀘스트 항목을 볼 수 있습니다.

만약 프로그램의 길이가 너무 길어서 타이핑이 힘들 경우 프로그램의 이름을 `dqchecker.exe`와 같이 짧게 수정하여 실행할 수도 있습니다.

`Path` 환경변수에 해당 프로그램 폴더의 위치를 추가하여 터미널을 열고 바로 실행할 수 있도록 할 수도 있습니다.

먼저, 아까 수정한 `daily-quest.default.txt`를 적용하기 위해 reset 명령어를 사용합니다.

```powershell
.\DailyQuestChecker.exe reset
```

초기화를 할 것이냐고 물어볼텐데, y를 입력해서 초기화를 하면 정상적으로 내용이 보이게 됩니다.

### 일일퀘스트 항목 체크 방법

check 명령어의 인자에 일일퀘스트 항목의 번호를 적으면 해당 항목이 체크 표시되거나 체크해제됩니다. 띄어쓰기로 구분하여서 한번에 여러개의 항목을 수정할 수 있습니다.

```powershell
.\DailyQuestChecker.exe check 2
.\DailyQuestChecker.exe check 1 3 4
```

### 일일퀘스트 체크 초기화

reset 명령어를 이용하여 바로 초기화하거나 자정 (밤 12시)가 지나서 다시 실행하면 일일퀘스트 항목이 초기화됩니다.
