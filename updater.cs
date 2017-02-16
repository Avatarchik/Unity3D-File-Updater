using UnityEngine;
using System.Collections;
using System.Diagnostics;
using System;
using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Linq;

public class StartOtherApp : MonoBehaviour
{
	Dictionary<string, string> filesClient = new Dictionary <string, string>(); 
	Dictionary<string, string> filesServer = new Dictionary <string, string>(); 
	string logMessage="";

    private Process pc;


	//总共要下载的bundle个数
	private int mTotalBundleCount = -1;
	//当前已下载的bundle个数
	private int mBundleCount = 0;

	protected string serverurl = "http://cdn.sinacloud.net/cosplay/Application/";
	protected string appPath = "";

	IEnumerator Start() {
		Application.RegisterLogCallback(HandleLog);
		print("开始检查软件更新");

		WWW www = new WWW(serverurl+"filesClient.txt"+"?time="+UnityEngine.Random.Range(0.1f,1000f));
		print (www.url);
		yield return www;

		if (www.error != null) {

			//0.net off ,Open the App
			//StartCoroutine(StartApp());
			print (www.error);

			print ("更新完成，正在启动中");
			StartCoroutine (StartApp());

		} 
		if (www.isDone) 
		{
			
			//0.net on , Check Update

			appPath = MyPath.Combine(Application.dataPath,"Application");

			ES2.SaveRaw(www.bytes, MyPath.Combine(appPath,"filesServer.txt"));

			GetDirs(appPath); 

			//1.get Local file
			saveFileDictionary (filesClient);
			print ("======fileClient======");
			string temp="";
			foreach (var i in filesClient)
			{
				temp += i;
				temp += "\n\r\n\r";
			}
			print (temp);


			//2.get server file
			print ("======fileServer======");
			filesServer = ES2.LoadDictionary<string, string>(MyPath.Combine(appPath,"filesServer.txt"));
			temp="";
			foreach (var i in filesServer)
			{
				temp += i;
				temp += "\n\r\n\r";
			}
			print (temp);


			//3.compare with file
			print ("======Server don't have Client have,Should Delete======");
			IEnumerable<string> filesDelete = filesClient.Keys.Except(filesServer.Keys);
			temp="";
			foreach (var i in filesDelete)
			{
				temp += i;
				temp += "\n\r\n\r";
				File.Delete(MyPath.Combine(appPath, filesClient[i] ));
			}
			print (temp);


			//4.compare with file
			print ("======Server have Client don't have,Should Download======");
			IEnumerable<string> filesDownload = filesServer.Keys.Except(filesClient.Keys);
			temp="";

			mTotalBundleCount = filesDownload.Count();

	
			foreach (var i in filesDownload)
			{
				temp += i;
				temp += "\n\r\n\r";

				StartCoroutine(CoDownloadAndWriteFile(MyPath.Combine(serverurl,filesServer[i]), MyPath.Combine(appPath,filesServer[i])));
			}
			print (temp);

			//5.检查是否下载完毕
			StartCoroutine(CheckLoadFinish());
		}
	}
		


	//start app
    IEnumerator StartApp()
    {
		pc = Process.Start(appPath+"main.exe");
		yield return new WaitForSeconds(2.0f);
		Application.Quit();
    }

	//save local file
	void saveFileDictionary(Dictionary<string, string> myDictionary){
		ES2.Save(myDictionary, MyPath.Combine(appPath,"filesClient.txt"));
	}

	//save server file
	void saveServerData(Byte[] bytes){
		ES2.SaveRaw(bytes, MyPath.Combine(appPath,"filesServer.txt"));
	}

	//download File
	IEnumerator downloadFile(string url){
		yield return null;
	}

	//get file md5
	public string getFileHash(string filePath)
	{           
		try
		{
			FileStream fs = new FileStream(filePath, FileMode.Open);
			int len = (int)fs.Length;
			byte[] data = new byte[len];
			fs.Read(data, 0, len);
			fs.Close();
			MD5 md5 = new MD5CryptoServiceProvider();
			byte[] result = md5.ComputeHash(data);
			string fileMD5 = "";
			foreach (byte b in result)
			{
				fileMD5 += Convert.ToString(b, 16);
			}
			return fileMD5;   
		}
		catch (FileNotFoundException e)
		{
			print(e.Message);
			return "";
		}                                 
	}

	//get dir
	private void GetDirs(string dirPath)  
	{  
		foreach (string path in Directory.GetFiles(dirPath))  
		{  
			//获取所有文件夹中包含后缀为 .cs 的路径  
			if ( !System.IO.Path.GetExtension(path).Contains(".meta") &&  !System.IO.Path.GetExtension(path).Contains("DS_Store")   )  
			{  
				try{
					filesClient.Add(getFileHash(path),path.Replace(appPath,""));
				}catch(System.Exception e){
					print(e);
				}
			}  
		}  
		if (Directory.GetDirectories(dirPath).Length > 0)  //遍历所有文件夹  
		{  
			foreach (string path in Directory.GetDirectories(dirPath))  
			{  
				GetDirs(path);  
			}  
		}  
	}

	//下载并写入文件
	private IEnumerator CoDownloadAndWriteFile(string url,string filePath)
	{

		print ("======="+url+"======="+filePath);

		yield return null;

		using (WWW www = new WWW(url))
		{
			yield return www;

			if (www.error != null)
			{
				UnityEngine.Debug.Log("[download error]"+string.Format("Read {0} failed: {1}", url, www.error));
				mBundleCount++;
				yield break;
			}

			ES2.SaveRaw(www.bytes, filePath);

			UnityEngine.Debug.Log("[download start]" + url);

			www.Dispose();

			mBundleCount++;

			print("当前已经下载文件的数量为"+mBundleCount+" total="+mTotalBundleCount);

		}
	}

	//递归创建文件夹
	public static string CreateDirectoryRecursive(string relativePath)
	{
		var list = relativePath.Split('/');
		var temp = "";
		for (int i=0;i<list.Length-1;i++)
		{
			var dir = list[i];
			if (string.IsNullOrEmpty(dir))
			{
				continue;
			}
			temp += "/" + dir;
			if (!Directory.Exists(temp))
			{
				try{
					Directory.CreateDirectory(temp);
				}catch(SystemException e){}
			}
		}
		return temp;
	}

	//检查是否已经下载完毕
	IEnumerator CheckLoadFinish()
	{
		while (mBundleCount < mTotalBundleCount)
		{
			yield return null;
		}

		print("所有文件下载完成");

		print ("更新完成，正在启动中");
		StartCoroutine (StartApp());

	}
	//自己写的Path.Combines
	class MyPath
	{
		public static string Combine(params string[] paths)
		{
			if (paths.Length == 0)
			{
				throw new ArgumentException("please input path");
			}
			else
			{
				StringBuilder builder = new StringBuilder();
				//string spliter = "\\";
				string spliter = "/";
				string firstPath = paths[0];
				if (firstPath.StartsWith("HTTP", StringComparison.OrdinalIgnoreCase))
				{
					spliter = "/";
				}
				if (!firstPath.EndsWith(spliter))
				{
					firstPath = firstPath + spliter;
				}
				builder.Append(firstPath);
				for (int i = 1; i < paths.Length; i++)
				{
					string nextPath = paths[i];
					if (nextPath.StartsWith("/") || nextPath.StartsWith("\\"))
					{
						nextPath = nextPath.Substring(1);
					}
					if (i != paths.Length - 1)//not the last one
					{
						if (nextPath.EndsWith("/") || nextPath.EndsWith("\\"))
						{
							nextPath = nextPath.Substring(0, nextPath.Length - 1) + spliter;
						}
						else
						{
							nextPath = nextPath + spliter;
						}
					}
					builder.Append(nextPath);
				}
				return builder.ToString();
			}
		}
	}

	//HandleLog
	void HandleLog(string logString, string stackTrace, LogType type)
	{
		logMessage = logString;
		if (type == LogType.Error || type == LogType.Exception) 
		{
		}
	}
	//GUI
	void OnGUI()
	{
		GUI.color = Color.black;
		GUILayout.Label(logMessage);
	}

}
