﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Client;
using IdentityServer4.UserServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ProjsctThis.Model.ApiModel;
using SPA_NetCore_Foundation.Global;
using SPA_NetCore_Foundation.Model;
using ModelDB;
using WebApiAuth.Model.Sign;

namespace SPA_NetCore_Foundation.Controllers
{
    /// <summary>
    /// 사인 관련(인,아웃,조인)
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class SignController : ControllerBase
    {
        private readonly string ClientId = "resourceownerclient";
        private readonly string ClientSecret = "dataEventRecordsSecret";
        private readonly string Scope = "dataEventRecords offline_access openid";


        /// <summary>
        /// 사인인 시도
        /// </summary>
        /// <param name="sEmail"></param>
        /// <param name="sPW"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("SignIn")]
        public ActionResult<SignInResultModel> SignIn(
            [FromForm]string sEmail
            , [FromForm]string sPW)
        {
            //결과용
            ApiResultReadyModel armResult = new ApiResultReadyModel(this);
            //로그인 처리용 모델
            SignInResultModel smResult = new SignInResultModel();

            //검색된 유저
            User user = null;

            using (SpaNetCoreFoundationContext db1 = new SpaNetCoreFoundationContext(GlobalStatic.DBMgr.DbContext_Opt()))
            {
                //유저 검색
                user
                    = db1.User
                        .FirstOrDefault(m =>
                            m.SignEmail == sEmail
                            && m.Password == sPW);
            }


            if (user != null)
            {
                //토큰 요청
                TokenResponse tr = RequestTokenAsync(sEmail, sPW).Result;

                if (true == tr.IsError)
                {//에러가 있다.
                    armResult.infoCode = "1";
                    armResult.message = "아이디나 비밀번호가 틀렸습니다.";

                    armResult.StatusCode = StatusCodes.Status401Unauthorized;
                }
                else
                {//에러가 없다.
                    //로그인 되어있는 유저정보 저장
                    GlobalStatic.SignInList.Add(user.idUser, tr.RefreshToken);

                    smResult.id = user.idUser;
                    smResult.email = user.SignEmail;

                    smResult.lv = 0;

                    smResult.access_token = tr.AccessToken;
                    smResult.refresh_token = tr.RefreshToken;
                }
            }
            else
            {
                armResult.infoCode = "1";
                armResult.message = "아이디나 비밀번호가 틀렸습니다.";

                armResult.StatusCode = StatusCodes.Status401Unauthorized;
            }

            return armResult.ToResult(smResult);
        }

        /// <summary>
        /// 지정된 토큰을 찾아 지운다.
        /// </summary>
        /// <param name="sRefreshToken"></param>
        /// <returns></returns>
        [Authorize]//OAuth2 인증 설정
        [HttpPut]
        [Route("SignOut")]
        public ActionResult<string> SignOut(
            [FromForm]int nID
            , [FromForm]string sRefreshToken)
        {
            ApiResultReadyModel armResult = new ApiResultReadyModel(this);
            ApiResultBaseModel arbm = new ApiResultBaseModel();

            //사인아웃에 필요한 작업을 한다.
            //사용자
            GlobalStatic.SignInList.Delete(nID, sRefreshToken);

            //리플레시 토큰 제거
            if ((null != sRefreshToken)
                && (string.Empty != sRefreshToken))
            {
                TokenRevocationResponse trr = RevocationTokenAsync(sRefreshToken).Result;
            }
            
            //로컬 인증 쿠키 삭제 요청
            HttpContext.SignOutAsync();

            //임시로 아이디를 넘긴다.
            return armResult.ToResult(arbm);
        }



        /// <summary>
        /// 인증에 사용할  http클라이언트
        /// </summary>
        private HttpClient hcAuthClient = new HttpClient();
        /// <summary>
        /// IdentityServer4로 구현된 서버 주소
        /// </summary>
        private string sIdentityServer4_Url = GlobalStatic.AuthUrl;

        

        [HttpPut]
        [Route("RefreshToAccess")]
        public ActionResult<SignInResultModel> RefreshToAccess(
            [FromForm]string sRefreshToken)
        {
            //결과용
            ApiResultReadyModel armResult = new ApiResultReadyModel(this);
            //엑세스 토큰 갱신용 모델
            SignInResultModel smResult = new SignInResultModel();

            //토큰 갱신 요청
            TokenResponse tr = RefreshTokenAsync(sRefreshToken).Result;

            if (true == tr.IsError)
            {//에러가 있다.
                armResult.infoCode = "1";
                armResult.message = "토큰 갱신에 실패하였습니다.";

                armResult.StatusCode = StatusCodes.Status401Unauthorized;
            }
            else
            {//에러가 없다.
                //유저 정보를 받는다.
                UserInfoResponse inrUser 
                    = UserInfoAsync(smResult.access_token).Result;

                //유저 정보 추출
                ClaimModel cm = new ClaimModel(inrUser.Claims);

                //로그인 되어있는 유저정보 저장
                GlobalStatic.SignInList.Add(cm.id_int, tr.RefreshToken);


                //모델에 입력
                smResult.id = cm.id_int;
                smResult.email = cm.email;

                smResult.access_token = tr.AccessToken;
                smResult.refresh_token = tr.RefreshToken;
            }

            return armResult.ToResult(smResult);
        }



        /// <summary>
        /// 인증서버에 인증을 요청한다.
        /// </summary>
        /// <param name="sID"></param>
        /// <param name="sPassword"></param>
        /// <returns></returns>
        private async Task<TokenResponse> RequestTokenAsync(string sID, string sPassword)
        {
            TokenResponse trRequestToken
                = await hcAuthClient
                        .RequestPasswordTokenAsync(new PasswordTokenRequest
                        {
                            Address = this.sIdentityServer4_Url + "connect/token",

                            ClientId = this.ClientId,
                            ClientSecret = this.ClientSecret,
                            Scope = this.Scope,

                            //유저 인증정보 : 아이디
                            UserName = sID,
                            //유저 인증정보 : 비밀번호
                            Password = sPassword
                        });

            return trRequestToken;
        }

        /// <summary>
        /// 액세스 토큰 갱신
        /// </summary>
        /// <param name="sRefreshToken">리플레시토큰</param>
        /// <returns></returns>
        private async Task<TokenResponse> RefreshTokenAsync(string sRefreshToken)
        {
            TokenResponse trRequestToken
                = await hcAuthClient
                        .RequestRefreshTokenAsync(new RefreshTokenRequest
                        {
                            Address = this.sIdentityServer4_Url + "connect/token",

                            ClientId = this.ClientId,
                            ClientSecret = this.ClientSecret,
                            Scope = this.Scope,

                            RefreshToken = sRefreshToken
                        });

            return trRequestToken;
        }


        /// <summary>
        /// 지정된 토큰 제거
        /// </summary>
        /// <param name="sRefreshToken"></param>
        /// <returns></returns>
        private async Task<TokenRevocationResponse> RevocationTokenAsync(string sRefreshToken)
        {
            //엑세스 토큰도 제거가 가능하지만
            //이 시나리오에서는 리플레시 토큰만 제거하면 된다.
            TokenRevocationResponse trRequestToken
                = await hcAuthClient
                        .RevokeTokenAsync(new TokenRevocationRequest
                        {
                            Address = this.sIdentityServer4_Url + "connect/revocation",
                            ClientId = this.ClientId,
                            ClientSecret = this.ClientSecret,

                            Token = sRefreshToken,
                            TokenTypeHint = "refresh_token"
                        });

            return trRequestToken;
        }

        /// <summary>
        /// 엑세스토큰을 이용하여 유저 정보를 받는다.
        /// </summary>
        /// <param name="sAccessToken"></param>
        /// <returns></returns>
        private async Task<UserInfoResponse> UserInfoAsync(string sAccessToken)
        {
            UserInfoResponse uirUser
                = await hcAuthClient
                        .GetUserInfoAsync(new UserInfoRequest
                        {
                            Address = this.sIdentityServer4_Url + "connect/userinfo"

                            , Token = sAccessToken,
                        });

            return uirUser;
        }


    }
}