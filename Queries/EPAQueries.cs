using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;


namespace BookingAppV2.Queries
{
    public static class EPAQueries
    {

        //PerBrangay
        public static string PerBarangayQuery()
        {
            return @"SELECT	
                 		sum(x.ACTIVE_USER) ACTIVE_USER
                 		,sum(x.PREMIUM_USER) PREMIUM_USER
                 FROM	(SELECT	(SELECT top 1 CASE 
                 						WHEN Location_CV = 'LOCTN-699' THEN 'Outside Cotabato'
                 						when Location_CV = '' then 'No Address'
                 						when City = '' then 'No Address'
                 						else City
                 						end 
                 				FROM matrix.MemberProfile mp WHERE c.Member_AutoID = mp.Member_AutoID) 'Location'
                 				,CASE 
                 					WHEN sum(t.TotalSpent) >= 2000 THEN '1'
                 					ELSE 0
                 				END ACTIVE_USER
                 				,CASE 
                 					WHEN sum(t.TotalSpent) < 2000 THEN '1'
                 					ELSE 0
                 				END PREMIUM_USER
                 		FROM	matrixcrm.Card c
                 		INNER JOIN matrixcrm.Transact t
                 		ON		c.AutoID = t.Card_AutoID
                 		WHERE	TransactDate BETWEEN @startDate AND @endDate
                 		AND		BusinessEntity_Code != '98'
                 		AND t.CardType_Code = '07' 
                 		GROUP BY c.Member_AutoID
                 		) x";
        }

        //Kanegosyo
        public static string KanegosyoQuery()
        {
            return @"SELECT	
                      SUM(x.ACTIVE_USER)   AS ACTIVE_USER,
                      SUM(x.PREMIUM_USER)  AS PREMIUM_USER
                  FROM (
                      SELECT	
                          (
                              SELECT TOP 1 
                                  CASE 
                                      WHEN Location_CV = 'LOCTN-699' THEN 'Outside Cotabato'
                                      WHEN Location_CV = '' THEN 'No Address'
                                      WHEN City = '' THEN 'No Address'
                                      ELSE City
                                  END
                              FROM matrix.MemberProfile mp 
                              WHERE c.Member_AutoID = mp.Member_AutoID
                          ) AS Location,
                  
                          CASE 
                              WHEN SUM(t.TotalSpent) >= 2000 THEN 1
                              ELSE 0
                          END AS ACTIVE_USER,
                  
                          CASE 
                              WHEN SUM(t.TotalSpent) < 2000 THEN 1
                              ELSE 0
                          END AS PREMIUM_USER
                  
                      FROM matrixcrm.Card c
                      INNER JOIN matrixcrm.Transact t
                          ON c.AutoID = t.Card_AutoID
                      WHERE t.TransactDate BETWEEN @startDate AND @endDate
                        AND t.BusinessEntity_Code != '98'
                        AND t.CardType_Code = '06'
                      GROUP BY c.Member_AutoID
                  ) x";
        }




        //CotabatoCity
        public static string CotCityQuery()
        {
            return @"select x1._location
        		,x1.[TOTAL MEMBERS]
        		,x1.[NEW MEMBER]
        		,ISNULL(x2._active,0) 'ACTIVE BUYERS'
        from	(SELECT	Location _location
        		,SUM(case Bonus 
        			when 0.00000001 then 1
        			else 0
        		end) 'NEW MEMBER'
        		,SUM(case Bonus 
        			when 0.0001 then 1
        			when 0.00000001 then 1
        			when 0 then 1
        			else 0
        		end)  'TOTAL MEMBERS'
        FROM	(SELECT	(SELECT top 1 CASE 
        											WHEN Location_CV = 'LOCTN-699' THEN 'Outside Cotabato'
        											when City = 'COTABATO CITY' then (select [Description] from matrix.CodeValue where Code = mp.Location_CV)
        											when Location_CV = '' then 'No Address'
        											when City = '' then 'No Address'
        											else 'Outside Cotabato'
        											end 
        						FROM matrix.MemberProfile mp WHERE m.AutoID = mp.Member_AutoID) 'Location'
        			, case CAST(
        					case (SELECT	COUNT(*)
        							FROM	matrixcrm.Card
        							WHERE	CardStatus_Code != 'CDSTS-ACTIVE'
        							AND		EffectiveDate <= @endDate
        							AND		Member_AutoID = m.AutoID
        							)
        					WHEN 0 THEN 0
        					ELSE 1
        					END
        					AS varchar(10)
        					)
        					 +'|'+
        					CAST(
        						case (SELECT	COUNT(*)
        								FROM	matrixcrm.Card
        								WHERE	EffectiveDate BETWEEN @startDate and @endDate
        								AND		CardStatus_Code = 'CDSTS-ACTIVE'
        								AND		Member_AutoID = m.AutoID
        					) 
        					WHEN 0 THEN '0|0'
        					ELSE '1|0'
        					END
        					AS VARCHAR(8))
        			WHEN '0|0|0' THEN 0
        			WHEN '0|1|0' THEN 0.00000001
        			WHEN '1|0|0' THEN 1
        			WHEN '1|1|0' THEN 0.0001
        			END  Bonus
        			FROM	matrix.Member m
        		) x
        group by x.Location) x1
        left join (SELECT	Location _location
        					,count(*) _active
        			FROM	(SELECT	Member_AutoID
        							,(SELECT top 1 CASE 
        											WHEN Location_CV = 'LOCTN-699' THEN 'Outside Cotabato'
        											when City = 'COTABATO CITY' then (select [Description] from matrix.CodeValue where Code = mp.Location_CV)
        											when Location_CV = '' then 'No Address'
        											when City = '' then 'No Address'
        											else 'Outside Cotabato'
        											end 
        									FROM matrix.MemberProfile mp WHERE c.Member_AutoID = mp.Member_AutoID) 'Location'
        					FROM	matrixcrm.Card c
        					WHERE	AutoID IN (SELECT	Card_AutoID
        										FROM	matrixcrm.Transact
        										WHERE	TransactDate BETWEEN @startDate AND @endDate
        										AND		BusinessEntity_Code != '98'
        										AND		CardType_Code in ('06','07'))
        					GROUP BY c.Member_AutoID
        					) x
        			GROUP BY Location) x2
        on		x1._location = x2._location
        order by x1._location
           OPTION (RECOMPILE)";
        }


        // Municiaplity
        public static string MunicipalityQuery()
        {
            return @"select x1._location
            		,x1.[TOTAL MEMBERS]
            		,x1.[NEW MEMBER]
            		,ISNULL(x2._active,0) 'ACTIVE BUYERS'
            from	(SELECT	Location _location
            		,SUM(case Bonus 
            			when 0.00000001 then 1
            			else 0
            		end) 'NEW MEMBER'
            		,SUM(case Bonus 
            			when 0.0001 then 1
            			when 0.00000001 then 1
            			else 0
            		end)  'TOTAL MEMBERS'
            FROM	(SELECT	(SELECT top 1 CASE 
            											WHEN Location_CV = 'LOCTN-699' THEN 'Outside Cotabato'
            											when Location_CV = '' then 'No Address'
            											when City = '' then 'No Address'
            											else City
            											end 
            						FROM matrix.MemberProfile mp WHERE m.AutoID = mp.Member_AutoID) 'Location'
            			, case CAST(
            					case (SELECT	COUNT(*)
            							FROM	matrixcrm.Card
            							WHERE	CardStatus_Code != 'CDSTS-ACTIVE'
            							AND		EffectiveDate BETWEEN @startDate and @endDate
            							AND		Member_AutoID = m.AutoID
            							)
            					WHEN 0 THEN 0
            					ELSE 1
            					END
            					AS varchar(10)
            					)
            					 +'|'+
            					CAST(
            						case (SELECT	COUNT(*)
            								FROM	matrixcrm.Card
            								WHERE	EffectiveDate BETWEEN @startDate and @endDate
            								AND		CardStatus_Code = 'CDSTS-ACTIVE'
            								AND		Member_AutoID = m.AutoID
            					) 
            					WHEN 0 THEN '0|0'
            					ELSE '1|0'
            					END
            					AS VARCHAR(8))
            			WHEN '0|0|0' THEN 0
            			WHEN '0|1|0' THEN 0.00000001
            			WHEN '1|0|0' THEN 1
            			WHEN '1|1|0' THEN 0.0001
            			END  Bonus
            			FROM	matrix.Member m
            		) x
            group by x.Location) x1
            left join (SELECT	Location _location
            					,count(*) _active
            			FROM	(SELECT	Member_AutoID
            							,(SELECT top 1 CASE 
            											WHEN Location_CV = 'LOCTN-699' THEN 'Outside Cotabato'
            											when Location_CV = '' then 'No Address'
            											when City = '' then 'No Address'
            											else City
            											end 
            									FROM matrix.MemberProfile mp WHERE c.Member_AutoID = mp.Member_AutoID) 'Location'
            					FROM	matrixcrm.Card c
            					WHERE	AutoID IN (SELECT	Card_AutoID
            										FROM	matrixcrm.Transact
            										WHERE	TransactDate BETWEEN @startDate AND @endDate
            										AND		BusinessEntity_Code != '98'
            										AND		CardType_Code in ('06','07'))
            					GROUP BY c.Member_AutoID
            					) x
            			GROUP BY Location) x2
            on		x1._location = x2._location
            WHERE	x1._location in ('BARIRA'
            ,'BULDON'
            ,'DATU BLAH T. SINSUAT'
            ,'DATU ODIN SINSUAT'
            ,'KABUNTALAN'
            ,'MATANOG'
            ,'NORTHERN KABUNTALAN'
            ,'NORTH UPI'
            ,'PARANG'
            ,'SULTAN KUDARAT'
            ,'SULTAN MASTURA'
            ,'SULTAN SUMAGKA-TALITAY'
            ,'AMPATUAN'
            ,'BULUAN'
            ,'DATU ABDULLAH SANGKI'
            ,'DATU ANNGAL MIDTIMBANG'
            ,'DATU HOFFER AMPATUAN'
            ,'DATU MONTAWAL'
            ,'DATU PAGLAS'
            ,'DATU PIANG'
            ,'DATU SALIBO'
            ,'DATU SAUDI-AMPATUAN'
            ,'DATU UNSAY'
            ,'GENERAL SALIPADA K. PENDATUN'
            ,'GUINDULUNGAN'
            ,'MAMASAPANO'
            ,'MANGUDADATU'
            ,'PAGALUNGAN'
            ,'PAGLAT'
            ,'PANDAG'
            ,'RAJAH BUAYAN'
            ,'SHARIFF AGUAK'
            ,'SHARIFF SAYDONA MUSTAPHA'
            ,'SULTAN SA BARONGIS'
            ,'SOUTH UPI'
            ,'TALAYAN'
            ,'ALAMADA'
            ,'ALEOSAN'
            ,'ANTIPAS'
            ,'ARAKAN'
            ,'BANISILAN'
            ,'CARMEN'
            ,'KABACAN'
            ,'KIDAPAWAN'
            ,'LIBUNGAN'
            ,'MLANG'
            ,'MAGPET'
            ,'MAKILALA'
            ,'MATALAM'
            ,'MIDSAYAP'
            ,'PIGCAWAYAN'
            ,'PRES. ROXAS'
            ,'PIKIT'
            ,'TULUNAN'
            ,'Outside Cotabato'
            )
            ORDER BY x1._location
              OPTION (RECOMPILE)";
        }


        //DOS
        public static string DOSQuery()
        {
            return @"select x1._location
                  		,x1.[TOTAL MEMBERS]
                  		,x1.[NEW MEMBER]
                  		,ISNULL(x2._active,0) 'ACTIVE BUYERS'
                  from	(SELECT	Location _location
                  		,SUM(case Bonus 
                  			when 0.00000001 then 1
                  			else 0
                  		end) 'NEW MEMBER'
                  		,SUM(case Bonus 
                  			when 0.0001 then 1
                  			when 0.00000001 then 1
                  			when 0 then 1
                  			else 0
                  		end)  'TOTAL MEMBERS'
                  FROM	(SELECT	(SELECT top 1 CASE 
                  											WHEN Location_CV = 'LOCTN-699' THEN 'Outside Cotabato'
                  											when City = 'DATU ODIN SINSUAT' then (select [Description] from matrix.CodeValue where Code = mp.Location_CV)
                  											when Location_CV = '' then 'No Address'
                  											when City = '' then 'No Address'
                  											else 'Outside Cotabato'
                  											end 
                  						FROM matrix.MemberProfile mp WHERE m.AutoID = mp.Member_AutoID) 'Location'
                  			, case CAST(
                  					case (SELECT	COUNT(*)
                  							FROM	matrixcrm.Card
                  							WHERE	CardStatus_Code != 'CDSTS-ACTIVE'
                  							AND		EffectiveDate <= @endDate
                  							AND		Member_AutoID = m.AutoID
                  							)
                  					WHEN 0 THEN 0
                  					ELSE 1
                  					END
                  					AS varchar(10)
                  					)
                  					 +'|'+
                  					CAST(
                  						case (SELECT	COUNT(*)
                  								FROM	matrixcrm.Card
                  								WHERE	EffectiveDate BETWEEN @startDate and @endDate
                  								AND		CardStatus_Code = 'CDSTS-ACTIVE'
                  								AND		Member_AutoID = m.AutoID
                  					) 
                  					WHEN 0 THEN '0|0'
                  					ELSE '1|0'
                  					END
                  					AS VARCHAR(8))
                  			WHEN '0|0|0' THEN 0
                  			WHEN '0|1|0' THEN 0.00000001
                  			WHEN '1|0|0' THEN 1
                  			WHEN '1|1|0' THEN 0.0001
                  			END  Bonus
                  			FROM	matrix.Member m
                  		) x
                  group by x.Location) x1
                  left join (SELECT	Location _location
                  					,count(*) _active
                  			FROM	(SELECT	Member_AutoID
                  							,(SELECT top 1 CASE 
                  											WHEN Location_CV = 'LOCTN-699' THEN 'Outside Cotabato'
                  											when City = 'DATU ODIN SINSUAT' then (select [Description] from matrix.CodeValue where Code = mp.Location_CV)
                  											when Location_CV = '' then 'No Address'
                  											when City = '' then 'No Address'
                  											else 'Outside Cotabato'
                  											end 
                  									FROM matrix.MemberProfile mp WHERE c.Member_AutoID = mp.Member_AutoID) 'Location'
                  					FROM	matrixcrm.Card c
                  					WHERE	AutoID IN (SELECT	Card_AutoID
                  										FROM	matrixcrm.Transact
                  										WHERE	TransactDate BETWEEN @startDate AND @endDate
                  										AND		BusinessEntity_Code != '98'
                  										AND		CardType_Code in ('06','07'))
                  					GROUP BY c.Member_AutoID
                  					) x
                  			GROUP BY Location) x2
                  on		x1._location = x2._location
                  order by x1._location
          OPTION (RECOMPILE)";
        }

        //Parang
        public static string ParangQuery()
        {
            return @"select x1._location
		,x1.[TOTAL MEMBERS]
		,x1.[NEW MEMBER]
		,ISNULL(x2._active,0) 'ACTIVE BUYERS'
from	(SELECT	Location _location
		,SUM(case Bonus 
			when 0.00000001 then 1
			else 0
		end) 'NEW MEMBER'
		,SUM(case Bonus 
			when 0.0001 then 1
			when 0.00000001 then 1
			when 0 then 1
			else 0
		end)  'TOTAL MEMBERS'
FROM	(SELECT	(SELECT top 1 CASE 
											WHEN Location_CV = 'LOCTN-699' THEN 'Outside Cotabato'
											when City = 'PARANG' then (select [Description] from matrix.CodeValue where Code = mp.Location_CV)
											when Location_CV = '' then 'No Address'
											when City = '' then 'No Address'
											else 'Outside Cotabato'
											end 
						FROM matrix.MemberProfile mp WHERE m.AutoID = mp.Member_AutoID) 'Location'
			, case CAST(
					case (SELECT	COUNT(*)
							FROM	matrixcrm.Card
							WHERE	CardStatus_Code != 'CDSTS-ACTIVE'
							AND		EffectiveDate <= @endDate
							AND		Member_AutoID = m.AutoID
							)
					WHEN 0 THEN 0
					ELSE 1
					END
					AS varchar(10)
					)
					 +'|'+
					CAST(
						case (SELECT	COUNT(*)
								FROM	matrixcrm.Card
								WHERE	EffectiveDate BETWEEN @startDate and @endDate
								AND		CardStatus_Code = 'CDSTS-ACTIVE'
								AND		Member_AutoID = m.AutoID
					) 
					WHEN 0 THEN '0|0'
					ELSE '1|0'
					END
					AS VARCHAR(8))
			WHEN '0|0|0' THEN 0
			WHEN '0|1|0' THEN 0.00000001
			WHEN '1|0|0' THEN 1
			WHEN '1|1|0' THEN 0.0001
			END  Bonus
			FROM	matrix.Member m
		) x
group by x.Location) x1
left join (SELECT	Location _location
					,count(*) _active
			FROM	(SELECT	Member_AutoID
							,(SELECT top 1 CASE 
											WHEN Location_CV = 'LOCTN-699' THEN 'Outside Cotabato'
											when City = 'PARANG' then (select [Description] from matrix.CodeValue where Code = mp.Location_CV)
											when Location_CV = '' then 'No Address'
											when City = '' then 'No Address'
											else 'Outside Cotabato'
											end 
									FROM matrix.MemberProfile mp WHERE c.Member_AutoID = mp.Member_AutoID) 'Location'
					FROM	matrixcrm.Card c
					WHERE	AutoID IN (SELECT	Card_AutoID
										FROM	matrixcrm.Transact
										WHERE	TransactDate BETWEEN @startDate AND @endDate
										AND		BusinessEntity_Code != '98'
										AND		CardType_Code in ('06','07'))
					GROUP BY c.Member_AutoID
					) x
			GROUP BY Location) x2
on		x1._location = x2._location
order by x1._location
    OPTION (RECOMPILE)";
        }

        //South Upi
        public static string SouthUpiQuery()
        {
            return @"select x1._location
		,x1.[TOTAL MEMBERS]
		,x1.[NEW MEMBER]
		,ISNULL(x2._active,0) 'ACTIVE BUYERS'
from	(SELECT	Location _location
		,SUM(case Bonus 
			when 0.00000001 then 1
			else 0
		end) 'NEW MEMBER'
		,SUM(case Bonus 
			when 0.0001 then 1
			when 0.00000001 then 1
			when 0 then 1
			else 0
		end)  'TOTAL MEMBERS'
FROM	(SELECT	(SELECT top 1 CASE 
											WHEN Location_CV = 'LOCTN-699' THEN 'Outside Cotabato'
											when City = 'SOUTH UPI' then (select [Description] from matrix.CodeValue where Code = mp.Location_CV)
											when Location_CV = '' then 'No Address'
											when City = '' then 'No Address'
											else 'Outside Cotabato'
											end 
						FROM matrix.MemberProfile mp WHERE m.AutoID = mp.Member_AutoID) 'Location'
			, case CAST(
					case (SELECT	COUNT(*)
							FROM	matrixcrm.Card
							WHERE	CardStatus_Code != 'CDSTS-ACTIVE'
							AND		EffectiveDate <= @endDate
							AND		Member_AutoID = m.AutoID
							)
					WHEN 0 THEN 0
					ELSE 1
					END
					AS varchar(10)
					)
					 +'|'+
					CAST(
						case (SELECT	COUNT(*)
								FROM	matrixcrm.Card
								WHERE	EffectiveDate BETWEEN @startDate and @endDate
								AND		CardStatus_Code = 'CDSTS-ACTIVE'
								AND		Member_AutoID = m.AutoID
					) 
					WHEN 0 THEN '0|0'
					ELSE '1|0'
					END
					AS VARCHAR(8))
			WHEN '0|0|0' THEN 0
			WHEN '0|1|0' THEN 0.00000001
			WHEN '1|0|0' THEN 1
			WHEN '1|1|0' THEN 0.0001
			END  Bonus
			FROM	matrix.Member m
		) x
group by x.Location) x1
left join (SELECT	Location _location
					,count(*) _active
			FROM	(SELECT	Member_AutoID
							,(SELECT top 1 CASE 
											WHEN Location_CV = 'LOCTN-699' THEN 'Outside Cotabato'
											when City = 'SOUTH UPI' then (select [Description] from matrix.CodeValue where Code = mp.Location_CV)
											when Location_CV = '' then 'No Address'
											when City = '' then 'No Address'
											else 'Outside Cotabato'
											end 
									FROM matrix.MemberProfile mp WHERE c.Member_AutoID = mp.Member_AutoID) 'Location'
					FROM	matrixcrm.Card c
					WHERE	AutoID IN (SELECT	Card_AutoID
										FROM	matrixcrm.Transact
										WHERE	TransactDate BETWEEN @startDate AND @endDate
										AND		BusinessEntity_Code != '98'
										AND		CardType_Code in ('06','07'))
					GROUP BY c.Member_AutoID
					) x
			GROUP BY Location) x2
on		x1._location = x2._location
order by x1._location 
            OPTION (RECOMPILE)";
        }

        //North Upi
        public static string NorthUpiQuery()
        {
            return @"select x1._location
		,x1.[TOTAL MEMBERS]
		,x1.[NEW MEMBER]
		,ISNULL(x2._active,0) 'ACTIVE BUYERS'
from	(SELECT	Location _location
		,SUM(case Bonus 
			when 0.00000001 then 1
			else 0
		end) 'NEW MEMBER'
		,SUM(case Bonus 
			when 0.0001 then 1
			when 0.00000001 then 1
			when 0 then 1
			else 0
		end)  'TOTAL MEMBERS'
FROM	(SELECT	(SELECT top 1 CASE 
											WHEN Location_CV = 'LOCTN-699' THEN 'Outside Cotabato'
											when City = 'NORTH UPI' then (select [Description] from matrix.CodeValue where Code = mp.Location_CV)
											when Location_CV = '' then 'No Address'
											when City = '' then 'No Address'
											else 'Outside Cotabato'
											end 
						FROM matrix.MemberProfile mp WHERE m.AutoID = mp.Member_AutoID) 'Location'
			, case CAST(
					case (SELECT	COUNT(*)
							FROM	matrixcrm.Card
							WHERE	CardStatus_Code != 'CDSTS-ACTIVE'
							AND		EffectiveDate <= @endDate
							AND		Member_AutoID = m.AutoID
							)
					WHEN 0 THEN 0
					ELSE 1
					END
					AS varchar(10)
					)
					 +'|'+
					CAST(
						case (SELECT	COUNT(*)
								FROM	matrixcrm.Card
								WHERE	EffectiveDate BETWEEN @startDate and @endDate
								AND		CardStatus_Code = 'CDSTS-ACTIVE'
								AND		Member_AutoID = m.AutoID
					) 
					WHEN 0 THEN '0|0'
					ELSE '1|0'
					END
					AS VARCHAR(8))
			WHEN '0|0|0' THEN 0
			WHEN '0|1|0' THEN 0.00000001
			WHEN '1|0|0' THEN 1
			WHEN '1|1|0' THEN 0.0001
			END  Bonus
			FROM	matrix.Member m
		) x
group by x.Location) x1
left join (SELECT	Location _location
					,count(*) _active
			FROM	(SELECT	Member_AutoID
							,(SELECT top 1 CASE 
											WHEN Location_CV = 'LOCTN-699' THEN 'Outside Cotabato'
											when City = 'NORTH UPI' then (select [Description] from matrix.CodeValue where Code = mp.Location_CV)
											when Location_CV = '' then 'No Address'
											when City = '' then 'No Address'
											else 'Outside Cotabato'
											end 
									FROM matrix.MemberProfile mp WHERE c.Member_AutoID = mp.Member_AutoID) 'Location'
					FROM	matrixcrm.Card c
					WHERE	AutoID IN (SELECT	Card_AutoID
										FROM	matrixcrm.Transact
										WHERE	TransactDate BETWEEN @startDate AND @endDate
										AND		BusinessEntity_Code != '98'
										AND		CardType_Code in ('06','07'))
					GROUP BY c.Member_AutoID
					) x
			GROUP BY Location) x2
on		x1._location = x2._location
order by x1._location
           OPTION (RECOMPILE)";
        }




        //Sultan Kudarat
        public static string SultanKudaratQuery()
        {
            return @"select x1._location
		,x1.[TOTAL MEMBERS]
		,x1.[NEW MEMBER]
		,ISNULL(x2._active,0) 'ACTIVE BUYERS'
from	(SELECT	Location _location
		,SUM(case Bonus 
			when 0.00000001 then 1
			else 0
		end) 'NEW MEMBER'
		,SUM(case Bonus 
			when 0.0001 then 1
			when 0.00000001 then 1
			when 0 then 1
			else 0
		end)  'TOTAL MEMBERS'
FROM	(SELECT	(SELECT top 1 CASE 
											WHEN Location_CV = 'LOCTN-699' THEN 'Outside Cotabato'
											when City = 'SULTAN KUDARAT' then (select [Description] from matrix.CodeValue where Code = mp.Location_CV)
											when Location_CV = '' then 'No Address'
											when City = '' then 'No Address'
											else 'Outside Cotabato'
											end 
						FROM matrix.MemberProfile mp WHERE m.AutoID = mp.Member_AutoID) 'Location'
			, case CAST(
					case (SELECT	COUNT(*)
							FROM	matrixcrm.Card
							WHERE	CardStatus_Code != 'CDSTS-ACTIVE'
							AND		EffectiveDate <= @endDate
							AND		Member_AutoID = m.AutoID
							)
					WHEN 0 THEN 0
					ELSE 1
					END
					AS varchar(10)
					)
					 +'|'+
					CAST(
						case (SELECT	COUNT(*)
								FROM	matrixcrm.Card
								WHERE	EffectiveDate BETWEEN @startDate and @endDate
								AND		CardStatus_Code = 'CDSTS-ACTIVE'
								AND		Member_AutoID = m.AutoID
					) 
					WHEN 0 THEN '0|0'
					ELSE '1|0'
					END
					AS VARCHAR(8))
			WHEN '0|0|0' THEN 0
			WHEN '0|1|0' THEN 0.00000001
			WHEN '1|0|0' THEN 1
			WHEN '1|1|0' THEN 0.0001
			END  Bonus
			FROM	matrix.Member m
		) x
group by x.Location) x1
left join (SELECT	Location _location
					,count(*) _active
			FROM	(SELECT	Member_AutoID
							,(SELECT top 1 CASE 
											WHEN Location_CV = 'LOCTN-699' THEN 'Outside Cotabato'
											when City = 'SULTAN KUDARAT' then (select [Description] from matrix.CodeValue where Code = mp.Location_CV)
											when Location_CV = '' then 'No Address'
											when City = '' then 'No Address'
											else 'Outside Cotabato'
											end 
									FROM matrix.MemberProfile mp WHERE c.Member_AutoID = mp.Member_AutoID) 'Location'
					FROM	matrixcrm.Card c
					WHERE	AutoID IN (SELECT	Card_AutoID
										FROM	matrixcrm.Transact
										WHERE	TransactDate BETWEEN @startDate AND @endDate
										AND		BusinessEntity_Code != '98'
										AND		CardType_Code in ('06','07'))
					GROUP BY c.Member_AutoID
					) x
			GROUP BY Location) x2
on		x1._location = x2._location
order by x1._location
            OPTION (RECOMPILE)";
        }

        //Sultan Mastura
        public static string SultanMasturaQuery()
        {
            return @"select x1._location
		,x1.[TOTAL MEMBERS]
		,x1.[NEW MEMBER]
		,ISNULL(x2._active,0) 'ACTIVE BUYERS'
from	(SELECT	Location _location
		,SUM(case Bonus 
			when 0.00000001 then 1
			else 0
		end) 'NEW MEMBER'
		,SUM(case Bonus 
			when 0.0001 then 1
			when 0.00000001 then 1
			when 0 then 1
			else 0
		end)  'TOTAL MEMBERS'
FROM	(SELECT	(SELECT top 1 CASE 
											WHEN Location_CV = 'LOCTN-699' THEN 'Outside Cotabato'
											when City = 'SULTAN MASTURA' then (select [Description] from matrix.CodeValue where Code = mp.Location_CV)
											when Location_CV = '' then 'No Address'
											when City = '' then 'No Address'
											else 'Outside Cotabato'
											end 
						FROM matrix.MemberProfile mp WHERE m.AutoID = mp.Member_AutoID) 'Location'
			, case CAST(
					case (SELECT	COUNT(*)
							FROM	matrixcrm.Card
							WHERE	CardStatus_Code != 'CDSTS-ACTIVE'
							AND		EffectiveDate <= @endDate
							AND		Member_AutoID = m.AutoID
							)
					WHEN 0 THEN 0
					ELSE 1
					END
					AS varchar(10)
					)
					 +'|'+
					CAST(
						case (SELECT	COUNT(*)
								FROM	matrixcrm.Card
								WHERE	EffectiveDate BETWEEN @startDate and @endDate
								AND		CardStatus_Code = 'CDSTS-ACTIVE'
								AND		Member_AutoID = m.AutoID
					) 
					WHEN 0 THEN '0|0'
					ELSE '1|0'
					END
					AS VARCHAR(8))
			WHEN '0|0|0' THEN 0
			WHEN '0|1|0' THEN 0.00000001
			WHEN '1|0|0' THEN 1
			WHEN '1|1|0' THEN 0.0001
			END  Bonus
			FROM	matrix.Member m
		) x
group by x.Location) x1
left join (SELECT	Location _location
					,count(*) _active
			FROM	(SELECT	Member_AutoID
							,(SELECT top 1 CASE 
											WHEN Location_CV = 'LOCTN-699' THEN 'Outside Cotabato'
											when City = 'SULTAN MASTURA' then (select [Description] from matrix.CodeValue where Code = mp.Location_CV)
											when Location_CV = '' then 'No Address'
											when City = '' then 'No Address'
											else 'Outside Cotabato'
											end 
									FROM matrix.MemberProfile mp WHERE c.Member_AutoID = mp.Member_AutoID) 'Location'
					FROM	matrixcrm.Card c
					WHERE	AutoID IN (SELECT	Card_AutoID
										FROM	matrixcrm.Transact
										WHERE	TransactDate BETWEEN @startDate AND @endDate
										AND		BusinessEntity_Code != '98'
										AND		CardType_Code in ('06','07'))
					GROUP BY c.Member_AutoID
					) x
			GROUP BY Location) x2
on		x1._location = x2._location
order by x1._location
                     OPTION (RECOMPILE)";
        }

        //Talayan
        public static string TalayanQuery()
        {
            return @"select x1._location
		,x1.[TOTAL MEMBERS]
		,x1.[NEW MEMBER]
		,ISNULL(x2._active,0) 'ACTIVE BUYERS'
from	(SELECT	Location _location
		,SUM(case Bonus 
			when 0.00000001 then 1
			else 0
		end) 'NEW MEMBER'
		,SUM(case Bonus 
			when 0.0001 then 1
			when 0.00000001 then 1
			when 0 then 1
			else 0
		end)  'TOTAL MEMBERS'
FROM	(SELECT	(SELECT top 1 CASE 
											WHEN Location_CV = 'LOCTN-699' THEN 'Outside Cotabato'
											when City = 'TALAYAN' then (select [Description] from matrix.CodeValue where Code = mp.Location_CV)
											when Location_CV = '' then 'No Address'
											when City = '' then 'No Address'
											else 'Outside Cotabato'
											end 
						FROM matrix.MemberProfile mp WHERE m.AutoID = mp.Member_AutoID) 'Location'
			, case CAST(
					case (SELECT	COUNT(*)
							FROM	matrixcrm.Card
							WHERE	CardStatus_Code != 'CDSTS-ACTIVE'
							AND		EffectiveDate <= @endDate
							AND		Member_AutoID = m.AutoID
							)
					WHEN 0 THEN 0
					ELSE 1
					END
					AS varchar(10)
					)
					 +'|'+
					CAST(
						case (SELECT	COUNT(*)
								FROM	matrixcrm.Card
								WHERE	EffectiveDate BETWEEN @startDate and @endDate
								AND		CardStatus_Code = 'CDSTS-ACTIVE'
								AND		Member_AutoID = m.AutoID
					) 
					WHEN 0 THEN '0|0'
					ELSE '1|0'
					END
					AS VARCHAR(8))
			WHEN '0|0|0' THEN 0
			WHEN '0|1|0' THEN 0.00000001
			WHEN '1|0|0' THEN 1
			WHEN '1|1|0' THEN 0.0001
			END  Bonus
			FROM	matrix.Member m
		) x
group by x.Location) x1
left join (SELECT	Location _location
					,count(*) _active
			FROM	(SELECT	Member_AutoID
							,(SELECT top 1 CASE 
											WHEN Location_CV = 'LOCTN-699' THEN 'Outside Cotabato'
											when City = 'TALAYAN' then (select [Description] from matrix.CodeValue where Code = mp.Location_CV)
											when Location_CV = '' then 'No Address'
											when City = '' then 'No Address'
											else 'Outside Cotabato'
											end 
									FROM matrix.MemberProfile mp WHERE c.Member_AutoID = mp.Member_AutoID) 'Location'
					FROM	matrixcrm.Card c
					WHERE	AutoID IN (SELECT	Card_AutoID
										FROM	matrixcrm.Transact
										WHERE	TransactDate BETWEEN @startDate AND @endDate
										AND		BusinessEntity_Code != '98'
										AND		CardType_Code in ('06','07'))
					GROUP BY c.Member_AutoID
					) x
			GROUP BY Location) x2
on		x1._location = x2._location
order by x1._location
                    OPTION (RECOMPILE)";
        }

        //Datu Anggal Midtimbang
        public static string DAMQuery()
        {
            return @"DECLARE @end AS VARCHAR(8)
DECLARE @start AS VARCHAR(8)

SET @start = '20260101' -- bakit 0327?
SET @end = '20260130'

select x1._location
		,x1.[TOTAL MEMBERS]
		,x1.[NEW MEMBER]
		,ISNULL(x2._active,0) 'ACTIVE BUYERS'
from	(SELECT	Location _location
		,SUM(case Bonus 
			when 0.00000001 then 1
			else 0
		end) 'NEW MEMBER'
		,SUM(case Bonus 
			when 0.0001 then 1
			when 0.00000001 then 1
			when 0 then 1
			else 0
		end)  'TOTAL MEMBERS'
FROM	(SELECT	(SELECT top 1 CASE 
											WHEN Location_CV = 'LOCTN-699' THEN 'Outside Cotabato'
											when City Like '%Midtimbang%' then (select [Description] from matrix.CodeValue where Code = mp.Location_CV)
											when Location_CV = '' then 'No Address'
											when City = '' then 'No Address'
											else 'Outside Cotabato'
											end 
						FROM matrix.MemberProfile mp WHERE m.AutoID = mp.Member_AutoID) 'Location'
			, case CAST(
					case (SELECT	COUNT(*)
							FROM	matrixcrm.Card
							WHERE	CardStatus_Code != 'CDSTS-ACTIVE'
							AND		EffectiveDate <= @endDate
							AND		Member_AutoID = m.AutoID
							)
					WHEN 0 THEN 0
					ELSE 1
					END
					AS varchar(10)
					)
					 +'|'+
					CAST(
						case (SELECT	COUNT(*)
								FROM	matrixcrm.Card
								WHERE	EffectiveDate BETWEEN @startDate and @endDate
								AND		CardStatus_Code = 'CDSTS-ACTIVE'
								AND		Member_AutoID = m.AutoID
					) 
					WHEN 0 THEN '0|0'
					ELSE '1|0'
					END
					AS VARCHAR(8))
			WHEN '0|0|0' THEN 0
			WHEN '0|1|0' THEN 0.00000001
			WHEN '1|0|0' THEN 1
			WHEN '1|1|0' THEN 0.0001
			END  Bonus
			FROM	matrix.Member m
		) x
group by x.Location) x1
left join (SELECT	Location _location
					,count(*) _active
			FROM	(SELECT	Member_AutoID
							,(SELECT top 1 CASE 
											WHEN Location_CV = 'LOCTN-699' THEN 'Outside Cotabato'
											when City Like '%Midtimbang%' then (select [Description] from matrix.CodeValue where Code = mp.Location_CV)
											when Location_CV = '' then 'No Address'
											when City = '' then 'No Address'
											else 'Outside Cotabato'
											end 
									FROM matrix.MemberProfile mp WHERE c.Member_AutoID = mp.Member_AutoID) 'Location'
					FROM	matrixcrm.Card c
					WHERE	AutoID IN (SELECT	Card_AutoID
										FROM	matrixcrm.Transact
										WHERE	TransactDate BETWEEN @startDate AND @endDate
										AND		BusinessEntity_Code != '98'
										AND		CardType_Code in ('06','07'))
					GROUP BY c.Member_AutoID
					) x
			GROUP BY Location) x2
on		x1._location = x2._location
order by x1._location
                     OPTION (RECOMPILE)";
        }

        //Guindulungan
        public static string GUINDULUNGANQuery()
        {
            return @"DECLARE @end AS VARCHAR(8)
DECLARE @start AS VARCHAR(8)

SET @start = '20260101' -- bakit 0327?
SET @end = '20260130'

select x1._location
		,x1.[TOTAL MEMBERS]
		,x1.[NEW MEMBER]
		,ISNULL(x2._active,0) 'ACTIVE BUYERS'
from	(SELECT	Location _location
		,SUM(case Bonus 
			when 0.00000001 then 1
			else 0
		end) 'NEW MEMBER'
		,SUM(case Bonus 
			when 0.0001 then 1
			when 0.00000001 then 1
			when 0 then 1
			else 0
		end)  'TOTAL MEMBERS'
FROM	(SELECT	(SELECT top 1 CASE 
											WHEN Location_CV = 'LOCTN-699' THEN 'Outside Cotabato'
											when City Like '%GUINDULUNGAN%' then (select [Description] from matrix.CodeValue where Code = mp.Location_CV)
											when Location_CV = '' then 'No Address'
											when City = '' then 'No Address'
											else 'Outside Cotabato'
											end 
						FROM matrix.MemberProfile mp WHERE m.AutoID = mp.Member_AutoID) 'Location'
			, case CAST(
					case (SELECT	COUNT(*)
							FROM	matrixcrm.Card
							WHERE	CardStatus_Code != 'CDSTS-ACTIVE'
							AND		EffectiveDate <= @endDate
							AND		Member_AutoID = m.AutoID
							)
					WHEN 0 THEN 0
					ELSE 1
					END
					AS varchar(10)
					)
					 +'|'+
					CAST(
						case (SELECT	COUNT(*)
								FROM	matrixcrm.Card
								WHERE	EffectiveDate BETWEEN @startDate and @endDate
								AND		CardStatus_Code = 'CDSTS-ACTIVE'
								AND		Member_AutoID = m.AutoID
					) 
					WHEN 0 THEN '0|0'
					ELSE '1|0'
					END
					AS VARCHAR(8))
			WHEN '0|0|0' THEN 0
			WHEN '0|1|0' THEN 0.00000001
			WHEN '1|0|0' THEN 1
			WHEN '1|1|0' THEN 0.0001
			END  Bonus
			FROM	matrix.Member m
		) x
group by x.Location) x1
left join (SELECT	Location _location
					,count(*) _active
			FROM	(SELECT	Member_AutoID
							,(SELECT top 1 CASE 
											WHEN Location_CV = 'LOCTN-699' THEN 'Outside Cotabato'
											when City Like '%GUINDULUNGAN%' then (select [Description] from matrix.CodeValue where Code = mp.Location_CV)
											when Location_CV = '' then 'No Address'
											when City = '' then 'No Address'
											else 'Outside Cotabato'
											end 
									FROM matrix.MemberProfile mp WHERE c.Member_AutoID = mp.Member_AutoID) 'Location'
					FROM	matrixcrm.Card c
					WHERE	AutoID IN (SELECT	Card_AutoID
										FROM	matrixcrm.Transact
										WHERE	TransactDate BETWEEN @startDate AND @endDate
										AND		BusinessEntity_Code != '98'
										AND		CardType_Code in ('06','07'))
					GROUP BY c.Member_AutoID
					) x
			GROUP BY Location) x2
on		x1._location = x2._location
order by x1._location
                    OPTION (RECOMPILE)";
        }

        //public static List<SqlParameter> DateParams(string start, string end)
        //{
        //    return new List<SqlParameter>
        //    {
        //        new SqlParameter("@startDate", start),
        //        new SqlParameter("@endDate", end)
        //    };
        //}
        public static List<SqlParameter> DateParams(string start, string end)
        {
            return new List<SqlParameter>
            {
                new SqlParameter("@startDate", SqlDbType.DateTime)
                {
                    Value = DateTime.ParseExact(start, "yyyyMMdd", null)
                },
                new SqlParameter("@endDate", SqlDbType.DateTime)
                {
                    Value = DateTime.ParseExact(end, "yyyyMMdd", null)
                }
            };
        }

    }
}
