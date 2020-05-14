using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class Test : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }
    // Start is called before the first frame update
    void Start()
    {
        DemoAsync();
    }
    void DebugGreen(string debugInfo)
    {
        Debug.Log($"<color=green>====={debugInfo}</color>");
    }
    public class TestNoCancel
    {
        public int a;
        public int b;
    }
    CancellationTokenSource cts = new CancellationTokenSource();
    async UniTask<string> DemoAsync()
    {
        //等资源加载完
        var asset = await Resources.LoadAsync<TextAsset>("foo");
        //等场景加载完
        await SceneManager.LoadSceneAsync("scene2").ConfigureAwait(Progress.Create<float>(x => DebugGreen($"x:{x}")));
        //等100帧
        TestNoCancel testNoCancel= new TestNoCancel() {a=1,b=2 };
        DebugGreen($"preFrameCount:{Time.frameCount}");
        await UniTask.DelayFrame(30,cancellationToken:cts.Token);

        testNoCancel = null;
        //调用cancellationtokensource 的Cancel方法来放在后边的异步方法启动
        cts.Cancel();//执行到后边有cancellationToken: cts.Token的地方之前，这样可以阻止异步突然点击退出按钮报错的问题
        
        await UniTask.DelayFrame(70, cancellationToken: cts.Token);
        DebugGreen($"afterFrameCount:{Time.frameCount}");
        DebugGreen($"testNoCancel:{testNoCancel.a} {testNoCancel.b}");
        //等10s
        DebugGreen($"preTime:{Time.realtimeSinceStartup}");
        await UniTask.Delay(TimeSpan.FromSeconds(10), ignoreTimeScale: true);
        DebugGreen($"afterTime:{Time.realtimeSinceStartup}");
        //等到当前帧结束
        await UniTask.Yield(PlayerLoopTiming.PostLateUpdate,cancellationToken: this.GetCancellationTokenOnDestroy());

        DebugGreen($"isActive false:{Time.realtimeSinceStartup}");
        bool isActive = false;
        Task.Run(() => {
            Thread.Sleep(5000);
            isActive = true;
            });
        //等知道当前变量为true
        await UniTask.WaitUntil(()=>isActive);
        DebugGreen($"isActive true:{Time.realtimeSinceStartup}");

        //等协程完成
        await FooCoroutineEnumerator();

        //等实际的一个线程完成，并且拿到一个100的返回值
        DebugGreen($"Tread Begin:{Time.realtimeSinceStartup}");
        await UniTask.Run(() => {
            Thread.Sleep(1000);
            return 100;
        } );
        DebugGreen($"Tread end:{Time.realtimeSinceStartup}");

        //同时操作
        async UniTask<string> GetTextAsync(UnityWebRequest req)
        {
            var op = await req.SendWebRequest();
            return op.downloadHandler.text;
        }

        var task1 = GetTextAsync(UnityWebRequest.Get("http://bing.com"));
        var task2 = GetTextAsync(UnityWebRequest.Get("http://baidu.com"));
        var task3 = GetTextAsync(UnityWebRequest.Get("https://workbench.umeng.com/"));
        // concurrent async-wait and get result easily by tuple syntax
        var (bing, baidu, yahoo) = await UniTask.WhenAll(task1,task2,task3);
        DebugGreen(bing);

        // You can handle timeout easily
        string unity=await GetTextAsync(UnityWebRequest.Get("http://unity.com")).Timeout(TimeSpan.FromMilliseconds(3000));
        DebugGreen(unity);

        return (asset as TextAsset)?.text ?? throw new InvalidOperationException("Asset not found");
    }

    IEnumerator FooCoroutineEnumerator()
    {
        DebugGreen($"preCoroutine:{Time.realtimeSinceStartup}");
        yield return new WaitForSeconds(1f);
        DebugGreen($"afterCoroutine:{Time.realtimeSinceStartup}");
    }
}
