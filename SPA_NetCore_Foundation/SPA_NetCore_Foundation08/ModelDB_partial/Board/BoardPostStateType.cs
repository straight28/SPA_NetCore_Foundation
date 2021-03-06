﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ModelDB
{
    /// <summary>
    /// 게시판 상태
    /// </summary>
    public enum BoardPostStateType
    {
        /// <summary>
        /// 없음
        /// </summary>
        None = 0,

        /// <summary>
        /// 정상
        /// </summary>
        Normal = 1,

        /// <summary>
        /// 삭제 함.
        /// 데몬으로 영구 삭제 예약
        /// </summary>
        Delete = 2,

        /// <summary>
        /// 관리자에 의해 블럭됨
        /// </summary>
        Block = 3,

        /// <summary>
        /// 전체 공지
        /// </summary>
        Notice_All = 1000,
        /// <summary>
        /// 그룹 공지
        /// </summary>
        Notice_Group = 1001,
        /// <summary>
        /// 해당 게시판 공지
        /// </summary>
        Notice_Board = 1002,
    }
}
