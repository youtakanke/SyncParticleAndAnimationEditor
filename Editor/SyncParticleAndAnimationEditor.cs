using UnityEngine;
using UnityEditor;

/// <summary>
/// Particle と Animationを同時に再生するエディター
/// 
/// startDelayが設定されていて、lifetimeが短いParticleは表示されないことがあります。
/// </summary>
public class SyncParticleAndAnimationEditor : EditorWindow
{
	[MenuItem ("Edit/Sync Particle and Animation Editor")]
	static void Init ()
	{
		var w = EditorWindow.GetWindow<SyncParticleAndAnimationEditor> ();
		w.Show ();
	}

	/// <summary>
	/// particleリスト
	/// </summary>
	ParticleSystem[] psList;

	/// <summary>
	/// animationリスト
	/// </summary>
	Animation[] animationsList;

	/// <summary>
	/// animatorリスト
	/// </summary>
	Animator[] animatorsList;

	/// <summary>
	/// simulate time.
	/// </summary>
	float simulateTime = 0f;
	/// <summary>
	/// effect time scale.
	/// </summary>
	float timeScale = 1f;
	/// <summary>
	/// old simulate time.
	/// sliderが変更されたかを見るための変数
	/// </summary>
	float old_simulateTime = 0f;
	/// <summary>
	/// Playボタンをおされたかどうか
	/// </summary>
	bool isPlaying = false;
	float playDeltaTime = 0f;
	/// <summary>
	/// 一番親階層の
	/// </summary>
	float rootDuration = 0;

	/// <summary>
	/// アニメーションクリップのナンバー
	/// </summary>
	int clipNumber = 0;

	/// <summary>
	/// 選択オブジェクトを固定にするか
	/// </summary>
	bool isSelectLock = false;
	/// <summary>
	/// 選択されたオブジェクト
	/// </summary>
	GameObject[] selectObjects;

	void OnGUI ()
	{
		//Lock解除されていたら、選択あれているものを表示
		if (!isSelectLock) {
			selectObjects = Selection.gameObjects;
		}

		if (selectObjects.Length == 0) {
			GUILayout.Label ("hierarchy上の対象となるGameObjectを選択してから\n再びこのウインドウをタッチしてください。");
		}

		foreach (GameObject go in selectObjects) {
			EditorGUILayout.BeginHorizontal ();
			{
				EditorGUILayout.ObjectField (go, typeof(GameObject), false, GUILayout.Width (188f));
				isSelectLock = GUILayout.Toggle (isSelectLock, "Lock");
			}
			EditorGUILayout.EndHorizontal ();

			rootDuration = 0;
			psList = go.GetComponentsInChildren<ParticleSystem> (true);
			animationsList = go.GetComponentsInChildren<Animation> (true);
			animatorsList = go.GetComponentsInChildren<Animator> (true);

			EditorGUILayout.BeginHorizontal ();
			{
				if (GUILayout.Button ("Prticle and Animation Play", GUILayout.Width (200))) {

					//下記のUpdateメソッド内でAnimationClipが再生される
					isPlaying = true;

					playDeltaTime = 0f;

					//particleだけは、ここでPlayで再生　（正確なタイムでシュミレートされるため）
					if (psList.Length > 0) {
						foreach (var ps in psList) {
							//ps.startDelay = 0;
							ps.Play ();
						}
					}
				}
				GUILayout.Label ("animation clip number : ", GUILayout.Width (140));

				//再生するAnimationClipの番号
				clipNumber = EditorGUILayout.IntField (clipNumber);
			}
			EditorGUILayout.EndHorizontal ();

			if (psList.Length > 0 || animationsList.Length > 0) {
				rootDuration = psList.Length > 0 ? psList [0].duration : 5f;
				GUILayout.Label ("Root Particle Duration Time = " + rootDuration.ToString () + "sec");
				simulateTime = EditorGUILayout.Slider ("Time Line", simulateTime, 0f, rootDuration);
				GUILayout.Label ("Animation speed 補正率　time × n     default = 1");
				timeScale = EditorGUILayout.Slider ("speed 調整", timeScale, 0f, 3f);

				//sliderの更新があるかないかをみる
				if (simulateTime == old_simulateTime) {
					continue;
				}
				old_simulateTime = simulateTime;
				isPlaying = false;

				// ParticleSystem再生
				PlayParticleSystem (simulateTime);

				// Animation再生
				if (animationsList != null) {
					PlayAnim (simulateTime);
				}

				// Animator再生より
				PlayAnimator (simulateTime);
			}

		}
	}

	void Update ()
	{
		if (isPlaying == true && playDeltaTime <= rootDuration) {
			playDeltaTime += timeScale * Time.fixedDeltaTime;
			PlayAnim (playDeltaTime);
			PlayAnimator (playDeltaTime);
		} else {
			isPlaying = false;
			playDeltaTime = 0f;
		}
	}

	/// <summary>
	/// ParticleSystemをシュミレートする
	/// </summary>
	/// <param name="time">Simulate Time.</param>
	void PlayParticleSystem (float time)
	{
		foreach (var p in psList) {
			float startDelay = 0;
//			p.startDelay = 0;
			p.Simulate (time + startDelay);
		}
	}

	/// <summary>
	/// Animator依存のAnimationClipをシュミレートする
	/// </summary>
	/// <param name="time">Simulate Time.</param>
	void PlayAnimator (float time)
	{
		foreach (var animator in animatorsList) {
			var ctl = animator.runtimeAnimatorController;
			if (ctl.animationClips.Length > 0) {
				ctl.animationClips [clipNumber].SampleAnimation (selectObjects [0], time);
			}
		}
	}

	/// <summary>
	/// Animation依存のAnimationClipをシュミレートする
	/// </summary>
	/// <param name="time">Simulate Time.</param>
	void PlayAnim (float time)
	{
		if (animationsList != null) {
			foreach (Animation childAnim in animationsList) {
				if (childAnim.clip != null) {
					childAnim.Play ();
					string cAnimName = childAnim.clip.name;
					childAnim [cAnimName].time = time;
					childAnim [cAnimName].enabled = true;
					childAnim.Sample ();
					childAnim [cAnimName].enabled = false;
				}
			}
		}
	}
}