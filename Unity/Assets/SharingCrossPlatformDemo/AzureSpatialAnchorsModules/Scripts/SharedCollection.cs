// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// // Copyright(c) 2019 Takahiro Miyaura
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php

using UnityEngine;

    /// <summary>
    /// This script exists as a stub to allow other scripts to find 
    /// the shared world anchor transform.
    /// </summary>
    public class SharedCollection : MonoBehaviour
    {
        /// <summary>
        /// 静的インスタンスフィールド
        /// </summary>
        private static SharedCollection _instance;

        /// <summary>
        /// Single instance of the anchor manager.
        /// Anchor管理クラスの唯一のインスタンスを取得します。
        /// </summary>
        public static SharedCollection Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SharedCollection>();
                }

                return _instance;
            }
        }

        public void Start()
        {
            DontDestroyOnLoad(Instance);
        }
    }
