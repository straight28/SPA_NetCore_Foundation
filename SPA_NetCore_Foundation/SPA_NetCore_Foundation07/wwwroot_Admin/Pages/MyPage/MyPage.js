﻿
function MyPage()
{
    GlobalStatic.PageType_Now = this.constructor.name;

    //페이지 공통기능 로드
    Page.Load({}, function () {
        //홈 인터페이스
        Page.divContents.load(FS_FUrl.MyPage_MyPageHtml
            , function ()
            {

            });
    });
}